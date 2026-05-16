using System.Globalization;
using System.Text.Json;
using SecretsClient.Core.Domain.Entities;
using SecretsClient.Core.Services;

namespace SecretsClient.Infrastructure.Geo;

public sealed class IpGeolocationLocationProvider : ICurrentLocationProvider
{
    private const string DefaultRequestUri = "https://ipapi.co/json/";
    private readonly HttpClient _httpClient;
    private readonly string _requestUri;

    public IpGeolocationLocationProvider(HttpClient httpClient)
        : this(httpClient, DefaultRequestUri)
    {
    }

    public IpGeolocationLocationProvider(HttpClient httpClient, string requestUri)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _requestUri = string.IsNullOrWhiteSpace(requestUri)
            ? throw new ArgumentException("Request URI must not be empty.", nameof(requestUri))
            : requestUri;
    }

    public async Task<GeoLocation?> GetCurrentLocationAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync(_requestUri, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return null;

            await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            var root = document.RootElement;

            if (root.TryGetProperty("status", out var status) &&
                status.ValueKind == JsonValueKind.String &&
                !string.Equals(status.GetString(), "success", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (root.TryGetProperty("success", out var success) &&
                success.ValueKind == JsonValueKind.False)
            {
                return null;
            }

            if (!TryReadCoordinate(root, "lat", "latitude", out var latitude) ||
                !TryReadCoordinate(root, "lon", "longitude", out var longitude))
            {
                return null;
            }

            var ipAddress = TryReadString(root, "query") ?? TryReadString(root, "ip");
            return GeoLocation.FromIpCoordinates(ipAddress, latitude, longitude);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryReadCoordinate(
        JsonElement root,
        string primaryProperty,
        string secondaryProperty,
        out double value)
    {
        if (TryReadDouble(root, primaryProperty, out value))
            return true;

        return TryReadDouble(root, secondaryProperty, out value);
    }

    private static bool TryReadDouble(JsonElement root, string propertyName, out double value)
    {
        value = default;

        if (!root.TryGetProperty(propertyName, out var property))
            return false;

        if (property.ValueKind == JsonValueKind.Number)
            return property.TryGetDouble(out value);

        if (property.ValueKind == JsonValueKind.String &&
            double.TryParse(
                property.GetString(),
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out value))
        {
            return true;
        }

        return false;
    }

    private static string? TryReadString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
            return null;

        return property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    }
}
