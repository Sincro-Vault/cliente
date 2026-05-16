namespace SecretsClient.Core.Domain.Entities;

public enum LocationSource
{
    Gps,
    IpAddress,
    WiFi,
    Manual
}

public sealed class GeoLocation
{
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public string? IpAddress { get; private set; }
    public string? WifiBssid { get; private set; }
    public DateTime Timestamp { get; private set; }
    public double AccuracyMeters { get; private set; }
    public LocationSource Source { get; private set; }
    public bool HasCoordinates { get; private set; }

    private GeoLocation() { }

    public static GeoLocation FromGps(double lat, double lon, double accuracyMeters)
        => Create(lat, lon, accuracyMeters, LocationSource.Gps);

    public static GeoLocation FromIpCoordinates(
        string? ipAddress,
        double lat,
        double lon,
        double accuracyMeters = 5000)
        => Create(lat, lon, accuracyMeters, LocationSource.IpAddress, ipAddress: ipAddress);

    public static GeoLocation FromManual(double lat, double lon, double accuracyMeters = 0)
        => Create(lat, lon, accuracyMeters, LocationSource.Manual);

    public static GeoLocation FromWiFi(
        double lat,
        double lon,
        string? wifiBssid,
        double accuracyMeters = 50)
        => Create(lat, lon, accuracyMeters, LocationSource.WiFi, wifiBssid: wifiBssid);

    public static GeoLocation FromIP(string ip)
    {
        return new GeoLocation
        {
            IpAddress = ip,
            Timestamp = DateTime.UtcNow,
            AccuracyMeters = 5000,
            Source = LocationSource.IpAddress,
            HasCoordinates = false
        };
    }

    private static GeoLocation Create(
        double lat,
        double lon,
        double accuracyMeters,
        LocationSource source,
        string? ipAddress = null,
        string? wifiBssid = null)
    {
        if (lat is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(lat), "Latitude must be between -90 and 90.");

        if (lon is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(lon), "Longitude must be between -180 and 180.");

        if (accuracyMeters < 0)
            throw new ArgumentOutOfRangeException(nameof(accuracyMeters), "Accuracy must be non-negative.");

        return new GeoLocation
        {
            Latitude = lat,
            Longitude = lon,
            IpAddress = ipAddress,
            WifiBssid = wifiBssid,
            Timestamp = DateTime.UtcNow,
            AccuracyMeters = accuracyMeters,
            Source = source,
            HasCoordinates = true
        };
    }
}
