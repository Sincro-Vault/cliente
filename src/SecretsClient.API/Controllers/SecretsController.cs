using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SecretsClient.Core.Services;
using SecretsClient.Core.DTOs;
using SecretsClient.Core.Domain.ValueObjects;

namespace SecretsClient.API.Controllers;

[ApiController]
[Route("api/secrets")]
[Authorize]
public class SecretsController : ControllerBase
{
    private readonly ISecretManager _secretManager;
    private readonly ILogger<SecretsController> _logger;

    public SecretsController(ISecretManager secretManager, ILogger<SecretsController> logger)
    {
        _secretManager = secretManager;
        _logger = logger;
    }

    /// <summary>
    /// Crea un secreto. Acepta el formato simplificado del frontend
    /// ({name, value, category, description}) o el formato completo del backend
    /// ({name, plaintextBase64, totalShares, thresholdShares, geoBoundaries}).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSecret([FromBody] SimpleCreateSecretRequest request, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { error = "El campo 'name' es requerido" });

            // Resolver el plaintext: viene como 'value' (texto plano del frontend) o 'plaintextBase64'.
            string plaintextBase64;
            if (!string.IsNullOrEmpty(request.PlaintextBase64))
            {
                plaintextBase64 = request.PlaintextBase64;
            }
            else if (!string.IsNullOrEmpty(request.Value))
            {
                plaintextBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(request.Value));
            }
            else
            {
                return BadRequest(new { error = "Falta 'value' o 'plaintextBase64'" });
            }

            var fullRequest = new CreateSecretRequest(
                Name: request.Name,
                PlaintextBase64: plaintextBase64,
                TotalShares: request.TotalShares ?? 2,
                ThresholdShares: request.ThresholdShares ?? 2,
                GeoBoundaries: request.GeoBoundaries ?? new List<GeoBoundaryDto>()
            );

            var secretId = await _secretManager.CreateSecretAsync(fullRequest, ct);
            return CreatedAtAction(nameof(GetSecret), new { id = secretId.ToString() }, new { id = secretId.ToString() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creando secreto");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// DTO flexible que acepta tanto el formato del frontend como el del backend.
    /// </summary>
    public record SimpleCreateSecretRequest(
        string Name,
        string? Value = null,
        string? Category = null,
        string? Description = null,
        string? PlaintextBase64 = null,
        int? TotalShares = null,
        int? ThresholdShares = null,
        List<GeoBoundaryDto>? GeoBoundaries = null);

    /// <summary>
    /// Lista secretos con paginación y filtros opcionales
    /// GET /api/secrets?search=&page=1&limit=8&category=
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListSecrets(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 8,
        [FromQuery] string? category = null,
        CancellationToken ct = default)
    {
        try
        {
            var all = (await _secretManager.ListSecretsAsync(ct)).ToList();

            // Filtro por búsqueda
            if (!string.IsNullOrWhiteSpace(search))
                all = all.Where(s => s.Name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

            var total = all.Count;
            var totalPages = (int)Math.Ceiling(total / (double)limit);
            var data = all.Skip((page - 1) * limit).Take(limit).ToList();

            return Ok(new { data, total, page, totalPages });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listando secretos");
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Reconstruye y devuelve el valor del secreto en RAM volátil.
    /// Valida geofencing si el secreto tiene políticas asociadas.
    /// POST /api/secrets/{id}/reveal
    /// Body opcional: { latitude, longitude, ipAddress, accuracyMeters }
    /// </summary>
    [HttpPost("{id}/reveal")]
    public async Task<IActionResult> RevealSecret(
        [FromRoute] string id,
        [FromBody] RevealRequest? request,
        CancellationToken ct)
    {
        try
        {
            GeoLocationDto? location = null;
            if (request != null && request.Latitude.HasValue && request.Longitude.HasValue)
            {
                location = new GeoLocationDto(
                    Latitude: request.Latitude.Value,
                    Longitude: request.Longitude.Value,
                    IpAddress: request.IpAddress ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                    WifiBssid: null,
                    Timestamp: DateTime.UtcNow,
                    AccuracyMeters: request.AccuracyMeters ?? 100,
                    Source: 3);
            }

            var revealed = await _secretManager.RevealSecretAsync(SecretId.From(id), location, ct);
            var plaintext = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(revealed.PlaintextBase64));
            return Ok(new
            {
                secretId = revealed.SecretId.ToString(),
                value = plaintext,
                reconstructedAt = revealed.ReconstructedAt
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Secreto no encontrado" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revelando secreto {Id}", id);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    public record RevealRequest(double? Latitude, double? Longitude, string? IpAddress, double? AccuracyMeters);

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSecret([FromRoute] string id, CancellationToken ct)
    {
        try
        {
            var secret = await _secretManager.GetSecretAsync(SecretId.From(id), ct);
            if (secret == null) return NotFound();
            return Ok(secret);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo secreto {Id}", id);
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Actualiza el nombre de un secreto existente
    /// PUT /api/secrets/:id
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSecret(
        [FromRoute] string id,
        [FromBody] UpdateSecretRequest request,
        CancellationToken ct)
    {
        try
        {
            var updated = await _secretManager.UpdateSecretAsync(SecretId.From(id), request, ct);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando secreto {Id}", id);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSecret([FromRoute] string id, CancellationToken ct)
    {
        try
        {
            await _secretManager.DeleteSecretAsync(SecretId.From(id), ct);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando secreto {Id}", id);
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Estadísticas del dashboard
    /// GET /api/secrets/stats
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        try
        {
            var secrets = (await _secretManager.ListSecretsAsync(ct)).ToList();
            return Ok(new
            {
                total = secrets.Count,
                active = secrets.Count(s => !s.IsLocked),
                lastAccess = DateTime.UtcNow,
                categories = new { general = secrets.Count }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo stats");
            return StatusCode(500, ex.Message);
        }
    }
}
