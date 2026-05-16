using SecretsClient.Core.Domain.Entities;

namespace SecretsClient.Core.Services;

public interface IGeoValidator
{
    Task<GeoValidationResult> ValidateLocationAsync(
        GeoLocation current,
        IReadOnlyCollection<GeoBoundary> authorizedBoundaries,
        CancellationToken ct = default);
}

public record GeoValidationResult(bool IsValid, string Reason, double? DistanceMeters = null);
