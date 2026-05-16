using SecretsClient.Core.Domain.Entities;
using SecretsClient.Core.Services;

namespace SecretsClient.Infrastructure.Geo;

public sealed class GeoValidator : IGeoValidator
{
    private const double EarthRadiusMeters = 6_371_000d;

    public Task<GeoValidationResult> ValidateLocationAsync(
        GeoLocation current,
        IReadOnlyCollection<GeoBoundary> authorizedBoundaries,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(authorizedBoundaries);
        ct.ThrowIfCancellationRequested();

        if (!current.HasCoordinates)
            return Task.FromResult(new GeoValidationResult(false, "Current location does not include coordinates."));

        if (authorizedBoundaries.Count == 0)
            return Task.FromResult(new GeoValidationResult(false, "No authorized geofences were configured."));

        double? nearestDistance = null;

        foreach (var boundary in authorizedBoundaries)
        {
            ValidateBoundary(boundary);

            var distance = CalculateDistanceMeters(
                current.Latitude,
                current.Longitude,
                boundary.Latitude,
                boundary.Longitude);

            nearestDistance = nearestDistance is null
                ? distance
                : Math.Min(nearestDistance.Value, distance);

            // We use a conservative margin: if the sensor accuracy could place the device
            // outside the boundary, access stays blocked.
            if (distance + current.AccuracyMeters <= boundary.RadiusMeters)
            {
                return Task.FromResult(
                    new GeoValidationResult(true, "Location is within an authorized boundary.", distance));
            }
        }

        return Task.FromResult(
            new GeoValidationResult(
                false,
                "Location is outside every authorized boundary once the accuracy margin is applied.",
                nearestDistance));
    }

    public double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        ValidateCoordinates(lat1, lon1, nameof(lat1), nameof(lon1));
        ValidateCoordinates(lat2, lon2, nameof(lat2), nameof(lon2));

        var latitudeDelta = DegreesToRadians(lat2 - lat1);
        var longitudeDelta = DegreesToRadians(lon2 - lon1);
        var originLatitude = DegreesToRadians(lat1);
        var destinationLatitude = DegreesToRadians(lat2);

        var a = Math.Pow(Math.Sin(latitudeDelta / 2), 2) +
                Math.Cos(originLatitude) *
                Math.Cos(destinationLatitude) *
                Math.Pow(Math.Sin(longitudeDelta / 2), 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    private static void ValidateBoundary(GeoBoundary boundary)
    {
        if (boundary.RadiusMeters <= 0)
            throw new InvalidOperationException("GeoBoundary radius must be greater than zero.");

        ValidateCoordinates(boundary.Latitude, boundary.Longitude, nameof(boundary.Latitude), nameof(boundary.Longitude));
    }

    private static void ValidateCoordinates(double latitude, double longitude, string latitudeName, string longitudeName)
    {
        if (double.IsNaN(latitude) || double.IsInfinity(latitude) || latitude is < -90 or > 90)
            throw new InvalidOperationException($"{latitudeName} must be between -90 and 90.");

        if (double.IsNaN(longitude) || double.IsInfinity(longitude) || longitude is < -180 or > 180)
            throw new InvalidOperationException($"{longitudeName} must be between -180 and 180.");
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;
}
