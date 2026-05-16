using SecretsClient.Core.Domain.Entities;
using SecretsClient.Core.DTOs;
using SecretsClient.Core.Services;

namespace SecretsClient.Infrastructure.Geo;

public sealed class GeoLocationService : IGeoLocationService
{
    private readonly ICurrentLocationProvider _currentLocationProvider;
    private readonly GpsLocationProvider _gpsLocationProvider;
    private readonly IpGeolocationLocationProvider _ipLocationProvider;

    public GeoLocationService(
        ICurrentLocationProvider currentLocationProvider,
        GpsLocationProvider gpsLocationProvider,
        IpGeolocationLocationProvider ipLocationProvider)
    {
        _currentLocationProvider = currentLocationProvider;
        _gpsLocationProvider = gpsLocationProvider;
        _ipLocationProvider = ipLocationProvider;
    }

    public async Task<GeoLocationDto> GetCurrentLocationAsync(CancellationToken ct)
    {
        var location = await _currentLocationProvider.GetCurrentLocationAsync(ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("No GPS or IP location source is currently available.");

        return ToDto(location);
    }

    public async Task<GeoLocationDto> GetGpsLocationAsync(CancellationToken ct)
    {
        var location = await _gpsLocationProvider.GetCurrentLocationAsync(ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("GPS location is not available.");

        return ToDto(location);
    }

    public async Task<GeoLocationDto> GetIpLocationAsync(CancellationToken ct)
    {
        var location = await _ipLocationProvider.GetCurrentLocationAsync(ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("IP geolocation is not available.");

        return ToDto(location);
    }

    private static GeoLocationDto ToDto(GeoLocation location)
    {
        return new GeoLocationDto(
            Latitude: location.Latitude,
            Longitude: location.Longitude,
            IpAddress: location.IpAddress ?? string.Empty,
            WifiBssid: location.WifiBssid,
            Timestamp: location.Timestamp,
            AccuracyMeters: location.AccuracyMeters,
            Source: (int)location.Source);
    }
}
