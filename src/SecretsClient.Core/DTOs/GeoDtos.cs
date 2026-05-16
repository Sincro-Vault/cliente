namespace SecretsClient.Core.DTOs;

/// <summary>
/// Ubicación geográfica actual del dispositivo
/// </summary>
public record GeoLocationDto(
    double Latitude,
    double Longitude,
    string IpAddress,
    string? WifiBssid,
    DateTime Timestamp,
    double AccuracyMeters,
    int Source); // 0=GPS, 1=IP, 2=WiFi

/// <summary>
/// Límite geográfico de autorización
/// </summary>
public record GeoBoundaryDto(
    double Latitude,
    double Longitude,
    double RadiusMeters,
    string Description);

/// <summary>
/// Resultado de validación geográfica
/// </summary>
public record GeoValidationResultDto(
    bool IsValid,
    string Reason,
    double? DistanceMeters);

/// <summary>
/// Solicitud para validar ubicación actual
/// </summary>
public record ValidateGeoRequest(
    GeoLocationDto Location,
    List<GeoBoundaryDto> Boundaries);

/// <summary>
/// Solicitud para establecer límites geográficos autorizados
/// </summary>
public record SetGeoBoundariesRequest(
    string SecretId,
    List<GeoBoundaryDto> Boundaries);
