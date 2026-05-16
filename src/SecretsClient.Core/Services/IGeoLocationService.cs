using SecretsClient.Core.DTOs;

namespace SecretsClient.Core.Services;

public interface IGeoLocationService
{
    /// <summary>
    /// Obtiene la ubicación actual del dispositivo
    /// Intenta GPS primero, fallback a IP Geolocation
    /// </summary>
    Task<GeoLocationDto> GetCurrentLocationAsync(CancellationToken ct);

    /// <summary>
    /// Obtiene ubicación solo por GPS
    /// </summary>
    Task<GeoLocationDto> GetGpsLocationAsync(CancellationToken ct);

    /// <summary>
    /// Obtiene ubicación por IP
    /// </summary>
    Task<GeoLocationDto> GetIpLocationAsync(CancellationToken ct);
}
