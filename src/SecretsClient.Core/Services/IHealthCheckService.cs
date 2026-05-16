namespace SecretsClient.Core.Services;

public interface IHealthCheckService
{
    Task<HealthStatusDto> CheckHealthAsync(CancellationToken ct);
    Task<StorageStatsDto> GetStorageStatsAsync(CancellationToken ct);
}

public record HealthStatusDto(
    string Status,
    string Version,
    DateTime CheckedAt,
    Dictionary<string, string> Components);

public record StorageStatsDto(
    int TotalSecrets,
    int TotalFragments,
    long DatabaseSizeBytes,
    long BlobStorageSizeBytes);
