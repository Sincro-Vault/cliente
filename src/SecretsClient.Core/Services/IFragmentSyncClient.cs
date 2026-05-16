using SecretsClient.Core.Domain.ValueObjects;

namespace SecretsClient.Core.Services;

/// <summary>
/// Comunicación cliente↔servidor para almacenar/recuperar el Fragmento B (F2).
/// En producción debe ser gRPC + TLS 1.3 (ver proto/secrets.proto del servidor).
/// La implementación actual usa REST sobre HTTP para simplificar.
/// </summary>
public interface IFragmentSyncClient
{
    Task UploadFragmentAsync(
        Guid userId,
        string username,
        SecretId secretId,
        int fragmentIndex,
        string encryptedFragmentBase64,
        string checksum,
        CancellationToken ct = default);

    /// <summary>
    /// Devuelve el fragmento base64 desde el servidor central, o null si no existe.
    /// </summary>
    Task<string?> DownloadFragmentAsync(
        Guid userId,
        SecretId secretId,
        CancellationToken ct = default);

    Task DeleteFragmentAsync(
        Guid userId,
        SecretId secretId,
        CancellationToken ct = default);
}
