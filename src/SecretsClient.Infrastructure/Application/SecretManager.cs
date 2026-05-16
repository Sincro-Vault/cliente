using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SecretsClient.Core.Domain.Entities;
using SecretsClient.Core.Domain.ValueObjects;
using SecretsClient.Core.DTOs;
using SecretsClient.Core.Services;

namespace SecretsClient.Infrastructure.Application;

public class SecretManager : ISecretManager
{
    private readonly ISecureStorage _storage;
    private readonly ISecretRepository _secretRepository;
    private readonly IShamirService _shamir;
    private readonly ICryptoService _crypto;
    private readonly IFragmentSyncClient _syncClient;
    private readonly IGeoValidator _geoValidator;
    private readonly IHttpContextAccessor _httpContext;
    private readonly ILogger<SecretManager> _logger;

    public SecretManager(
        ISecureStorage storage,
        ISecretRepository secretRepository,
        IShamirService shamir,
        ICryptoService crypto,
        IFragmentSyncClient syncClient,
        IGeoValidator geoValidator,
        IHttpContextAccessor httpContext,
        ILogger<SecretManager> logger)
    {
        _storage = storage;
        _secretRepository = secretRepository;
        _shamir = shamir;
        _crypto = crypto;
        _syncClient = syncClient;
        _geoValidator = geoValidator;
        _httpContext = httpContext;
        _logger = logger;
    }

    public async Task<SecretId> CreateSecretAsync(CreateSecretRequest request, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var userGuid = Guid.Parse(userId);
        var username = GetCurrentUsername();

        var plaintextBytes = Convert.FromBase64String(request.PlaintextBase64);

        // 1. Generar clave maestra + cifrar el payload con AES-256-GCM
        var masterKey = _crypto.GenerateSecureRandom(32);
        var encryptedPayload = await _crypto.EncryptAsync(plaintextBytes, masterKey, ct);

        // 2. Dividir la clave en N fragmentos con Shamir
        var shares = await _shamir.GenerateSharesAsync(masterKey, request.TotalShares, request.ThresholdShares, ct);
        if (shares.Count < 2)
            throw new InvalidOperationException("Se requieren al menos 2 fragmentos para distribución cliente/servidor");

        // 3. Persistir el secreto
        var secret = Secret.Create(request.Name, encryptedPayload, userId, userGuid);

        // 4. Guardar políticas de geofencing si vienen en el request
        if (request.GeoBoundaries != null)
        {
            foreach (var b in request.GeoBoundaries)
            {
                var policy = GeoPolicy.Create(secret.Id, b.Latitude, b.Longitude, b.RadiusMeters, b.Description);
                secret.AddGeoPolicy(policy);
            }
        }

        await _storage.SaveSecretAsync(secret, ct);

        // 5. F1 (índice 1) en cliente local
        var f1 = shares.First(s => s.Index == 1);
        var f1Checksum = _crypto.ComputeChecksum(f1.EncryptedShare);
        var f1Fragment = SecretFragment.Create(secret.Id, f1.Index, Convert.ToBase64String(f1.EncryptedShare), f1Checksum);
        await _storage.SaveFragmentAsync(f1Fragment, ct);

        // 6. F2 (índice 2) al servidor central via REST (rollback si falla)
        var f2 = shares.First(s => s.Index == 2);
        var f2Checksum = _crypto.ComputeChecksum(f2.EncryptedShare);

        try
        {
            await _syncClient.UploadFragmentAsync(
                userId: userGuid,
                username: username,
                secretId: secret.Id,
                fragmentIndex: f2.Index,
                encryptedFragmentBase64: Convert.ToBase64String(f2.EncryptedShare),
                checksum: f2Checksum,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Servidor central inalcanzable. Rollback del secreto {Id}", secret.Id);
            await _storage.DeleteSecretAsync(secret.Id, ct);
            throw new InvalidOperationException(
                "No se pudo enviar el Fragmento B al servidor central. El secreto fue revertido. " +
                "Verifica que el servidor esté disponible.", ex);
        }

        _logger.LogInformation(
            "Secreto '{Name}' fragmentado: F1 local, F2 enviado al servidor central",
            request.Name);
        return secret.Id;
    }

    public async Task<SecretResponse> GetSecretAsync(SecretId id, CancellationToken ct = default)
    {
        var secret = await _storage.GetSecretAsync(id, ct)
            ?? throw new KeyNotFoundException($"Secreto {id} no encontrado");

        return new SecretResponse(
            secret.Id, secret.Name, secret.CreatedAt, secret.UpdatedAt,
            secret.Fragments.Count, IsLocked: false);
    }

    public async Task<ReconstructedSecretResponse> RevealSecretAsync(
        SecretId id, GeoLocationDto? location, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var userGuid = Guid.Parse(userId);

        var secret = await _storage.GetSecretAsync(id, ct)
            ?? throw new KeyNotFoundException($"Secreto {id} no encontrado");

        // 1. Validación de geofencing si el secreto tiene políticas
        if (secret.GeoPolicies.Any())
        {
            if (location == null)
                throw new UnauthorizedAccessException("El secreto requiere ubicación para reconstruirse. Envía lat/lon.");

            var currentLocation = GeoLocation.FromManual(location.Latitude, location.Longitude, location.AccuracyMeters);
            var boundaries = secret.GeoPolicies
                .Select(p => new GeoBoundary(p.Latitude, p.Longitude, p.RadiusMeters))
                .ToList();
            var validation = await _geoValidator.ValidateLocationAsync(currentLocation, boundaries, ct);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Acceso DENEGADO por geofencing al secreto {Id}: {Reason}", id, validation.Reason);
                throw new UnauthorizedAccessException(
                    $"Ubicación no autorizada. {validation.Reason}. Distancia mínima: {validation.DistanceMeters:F0}m");
            }
            _logger.LogInformation("Geofencing OK para secreto {Id} (a {Dist:F0}m del perímetro)", id, validation.DistanceMeters);
        }

        // 2. Leer F1 local
        var localFragment = secret.Fragments.FirstOrDefault(f => f.FragmentIndex == 1)
            ?? throw new InvalidOperationException("Fragmento F1 local no encontrado");
        var f1Combined = Convert.FromBase64String(localFragment.EncryptedFragment);

        // 3. Pedir F2 al servidor central
        var f2Base64 = await _syncClient.DownloadFragmentAsync(userGuid, id, ct)
            ?? throw new InvalidOperationException(
                "Fragmento F2 no encontrado en el servidor central. " +
                "El secreto no se puede reconstruir.");
        var f2Combined = Convert.FromBase64String(f2Base64);

        // 4. Reconstruir las shares de Shamir.
        // El ShamirService embebe el header (threshold + shareIndex) en los primeros bytes
        // del EncryptedShare y deja Nonce vacío. Aquí simplemente devolvemos los payloads tal cual.
        var shares = new List<FragmentShare>
        {
            new(1, f1Combined, Array.Empty<byte>()),
            new(2, f2Combined, Array.Empty<byte>()),
        };

        // 5. Combinar para obtener la clave AES
        var masterKey = await _shamir.ReconstructSecretAsync(shares, ct);

        // 6. Descifrar el payload
        var encryptedPayload = Convert.FromBase64String(secret.EncryptedPayload);
        var plaintext = await _crypto.DecryptAsync(encryptedPayload, masterKey, ct);

        // 7. Zeroar la clave en memoria
        _crypto.ZeroMemory(masterKey);

        _logger.LogInformation("Secreto {Id} reconstruido exitosamente (en RAM volátil)", id);
        return new ReconstructedSecretResponse(
            SecretId: secret.Id,
            PlaintextBase64: Convert.ToBase64String(plaintext),
            ReconstructedAt: DateTime.UtcNow);
    }

    public async Task<IEnumerable<SecretResponse>> ListSecretsAsync(CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var secrets = await _secretRepository.GetAllByUserIdAsync(Guid.Parse(userId), ct);
        return secrets.Select(s => new SecretResponse(
            s.Id, s.Name, s.CreatedAt, s.UpdatedAt, s.Fragments.Count, IsLocked: false));
    }

    public async Task DeleteSecretAsync(SecretId id, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var userGuid = Guid.Parse(userId);

        // Borrar F2 en servidor central (best-effort, no falla si no responde)
        try
        {
            await _syncClient.DeleteFragmentAsync(userGuid, id, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo borrar F2 del servidor central para {Id}", id);
        }

        await _storage.DeleteSecretAsync(id, ct);
        _logger.LogInformation("Secreto {SecretId} eliminado", id);
    }

    public async Task<SecretResponse> UpdateSecretAsync(SecretId id, UpdateSecretRequest request, CancellationToken ct = default)
    {
        var secret = await _storage.GetSecretAsync(id, ct)
            ?? throw new KeyNotFoundException($"Secreto {id} no encontrado");

        if (!string.IsNullOrWhiteSpace(request.Name))
            secret.UpdateName(request.Name);

        secret.UpdateTimestamp();
        await _secretRepository.SaveChangesAsync(ct);

        _logger.LogInformation("Secreto {SecretId} actualizado", id);
        return new SecretResponse(secret.Id, secret.Name, secret.CreatedAt, secret.UpdatedAt, secret.Fragments.Count, IsLocked: false);
    }

    public Task RotateSecretAsync(SecretId id, RotationPolicy policy, CancellationToken ct = default)
    {
        throw new NotImplementedException("Rotación de secretos pendiente de implementación");
    }

    private string GetCurrentUserId()
    {
        var userId = _httpContext.HttpContext?.User?.FindFirstValue("sub")
            ?? _httpContext.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedAccessException("Usuario no autenticado");
        return userId;
    }

    private string GetCurrentUsername()
    {
        return _httpContext.HttpContext?.User?.FindFirstValue("username") ?? "unknown";
    }

    private static byte[] CombineNonceAndShare(byte[] nonce, byte[] share)
    {
        var combined = new byte[nonce.Length + share.Length];
        Buffer.BlockCopy(nonce, 0, combined, 0, nonce.Length);
        Buffer.BlockCopy(share, 0, combined, nonce.Length, share.Length);
        return combined;
    }
}
