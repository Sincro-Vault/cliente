using SecretsClient.Core.Domain.Entities;
using SecretsClient.Core.Services;

namespace SecretsClient.Infrastructure.Geo;

public sealed class CompositeLocationProvider : ICurrentLocationProvider
{
    private readonly IReadOnlyList<ICurrentLocationProvider> _providers;

    public CompositeLocationProvider(params ICurrentLocationProvider[] providers)
    {
        ArgumentNullException.ThrowIfNull(providers);
        _providers = providers.Where(provider => provider is not null).ToArray();
    }

    public async Task<GeoLocation?> GetCurrentLocationAsync(CancellationToken ct = default)
    {
        foreach (var provider in _providers)
        {
            ct.ThrowIfCancellationRequested();
            var location = await provider.GetCurrentLocationAsync(ct).ConfigureAwait(false);

            if (location is not null && location.HasCoordinates)
                return location;
        }

        return null;
    }
}
