using SecretsClient.Core.Domain.Entities;

namespace SecretsClient.Core.Services;

public interface ICurrentLocationProvider
{
    Task<GeoLocation?> GetCurrentLocationAsync(CancellationToken ct = default);
}
