using SecretsClient.Core.Domain.Entities;
using SecretsClient.Core.Domain.ValueObjects;
using SecretsClient.Core.DTOs;

namespace SecretsClient.Core.Services;

public interface ISecretManager
{
    Task<SecretId> CreateSecretAsync(CreateSecretRequest request, CancellationToken ct = default);
    Task<SecretResponse> GetSecretAsync(SecretId id, CancellationToken ct = default);
    Task<IEnumerable<SecretResponse>> ListSecretsAsync(CancellationToken ct = default);
    Task DeleteSecretAsync(SecretId id, CancellationToken ct = default);
    Task<SecretResponse> UpdateSecretAsync(SecretId id, UpdateSecretRequest request, CancellationToken ct = default);
    Task<ReconstructedSecretResponse> RevealSecretAsync(SecretId id, GeoLocationDto? location, CancellationToken ct = default);
    Task RotateSecretAsync(SecretId id, RotationPolicy policy, CancellationToken ct = default);
}

public record FragmentationPolicy(int TotalShares, int Threshold);

public record GeoBoundary(double Latitude, double Longitude, double RadiusMeters);

public record RotationPolicy(int ValidityDays, int NewTotalShares, int NewThreshold);
