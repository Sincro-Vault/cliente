using Microsoft.EntityFrameworkCore;
using SecretsClient.Core.Services;
using SecretsClient.Infrastructure.Data;

namespace SecretsClient.Infrastructure.Application;

public class HealthCheckService : IHealthCheckService
{
    private readonly SecretsDbContext _context;

    public HealthCheckService(SecretsDbContext context)
    {
        _context = context;
    }

    public async Task<HealthStatusDto> CheckHealthAsync(CancellationToken ct)
    {
        var components = new Dictionary<string, string>();
        var overallStatus = "healthy";

        try
        {
            await _context.Database.CanConnectAsync(ct);
            components["database"] = "healthy";
        }
        catch (Exception ex)
        {
            components["database"] = $"unhealthy: {ex.Message}";
            overallStatus = "degraded";
        }

        components["api"] = "healthy";

        return new HealthStatusDto(
            Status: overallStatus,
            Version: "1.0.0",
            CheckedAt: DateTime.UtcNow,
            Components: components);
    }

    public async Task<StorageStatsDto> GetStorageStatsAsync(CancellationToken ct)
    {
        var totalSecrets = await _context.Secrets.CountAsync(ct);
        var totalFragments = await _context.Fragments.CountAsync(ct);

        return new StorageStatsDto(
            TotalSecrets: totalSecrets,
            TotalFragments: totalFragments,
            DatabaseSizeBytes: 0,
            BlobStorageSizeBytes: 0);
    }
}
