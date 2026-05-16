using SecretsClient.Core.Domain.ValueObjects;

namespace SecretsClient.Core.DTOs;

/// <summary>
/// Solicitud para crear un nuevo secreto
/// </summary>
public record CreateSecretRequest(
    string Name,
    string PlaintextBase64,  // base64 del secreto en plano
    int TotalShares,
    int ThresholdShares,
    List<GeoBoundaryDto> GeoBoundaries);

/// <summary>
/// Respuesta con detalles del secreto (sin valor)
/// </summary>
public record SecretResponse(
    SecretId Id,
    string Name,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int FragmentCount,
    bool IsLocked);

/// <summary>
/// Solicitud para actualizar un secreto (nombre y descripción)
/// </summary>
public record UpdateSecretRequest(
    string? Name = null,
    string? Description = null,
    string? Category = null,
    string? NewPlaintextBase64 = null);

/// <summary>
/// Respuesta de recuperación del valor del secreto
/// </summary>
public record ReconstructedSecretResponse(
    SecretId SecretId,
    string PlaintextBase64,
    DateTime ReconstructedAt);
