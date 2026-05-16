namespace SecretsClient.Core.DTOs;

/// <summary>
/// Solicitud para enviar metadata de secreto local al servidor
/// </summary>
public record PushMetadataRequest(
    string SecretId,
    string ChecksumLocal,
    int FragmentCount,
    byte[] SignatureRsa);  // Firma RSA con clave privada del cliente

/// <summary>
/// Información de fragmento remoto descargado del servidor
/// </summary>
public record RemoteFragmentDto(
    string FragmentId,
    string ServerId,
    string LocationHint,
    byte[]? EncryptedFragment = null);

/// <summary>
/// Solicitud para sincronización periódica
/// </summary>
public record SyncRequest(
    string DeviceId,
    List<string> LocalSecretIds,
    byte[] SignatureRsa);

/// <summary>
/// Respuesta de sincronización del servidor
/// </summary>
public record SyncResponse(
    List<RemoteFragmentDto> RemoteFragments,
    List<string> DeletedSecretIds,
    DateTime SyncedAt);

public record SyncStatusResult(bool IsSuccess, string? FailureReason);
