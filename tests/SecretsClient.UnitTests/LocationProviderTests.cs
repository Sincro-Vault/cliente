using System.Net;
using System.Text;
using SecretsClient.Core.Domain.Entities;
using SecretsClient.Core.Services;
using SecretsClient.Infrastructure.Geo;

namespace SecretsClient.UnitTests;

public sealed class LocationProviderTests
{
    [Fact]
    public async Task IpGeolocationLocationProvider_ParsesCoordinatesFromIpApiResponse()
    {
        var handler = new StubHttpMessageHandler(
            """
            {
              "status": "success",
              "query": "203.0.113.5",
              "lat": 4.7110,
              "lon": -74.0721
            }
            """);
        var httpClient = new HttpClient(handler);
        var provider = new IpGeolocationLocationProvider(httpClient, "http://fake.test/json");

        var location = await provider.GetCurrentLocationAsync();

        Assert.NotNull(location);
        Assert.True(location!.HasCoordinates);
        Assert.Equal(LocationSource.IpAddress, location.Source);
        Assert.Equal(4.7110, location.Latitude, 4);
        Assert.Equal(-74.0721, location.Longitude, 4);
        Assert.Equal("203.0.113.5", location.IpAddress);
    }

    [Fact]
    public async Task CompositeLocationProvider_UsesFallbackWhenPrimaryReturnsNull()
    {
        var fallbackLocation = GeoLocation.FromIpCoordinates("203.0.113.5", 4.7110, -74.0721);
        var composite = new CompositeLocationProvider(
            new FakeLocationProvider(null),
            new FakeLocationProvider(fallbackLocation));

        var location = await composite.GetCurrentLocationAsync();

        Assert.NotNull(location);
        Assert.Equal(LocationSource.IpAddress, location!.Source);
    }

    [Fact]
    public async Task IpGeolocationLocationProvider_ParsesInvariantStringCoordinatesFromHttpsProviderSchema()
    {
        var handler = new StubHttpMessageHandler(
            """
            {
              "success": true,
              "ip": "203.0.113.7",
              "latitude": "4.7110",
              "longitude": "-74.0721"
            }
            """);
        var httpClient = new HttpClient(handler);
        var provider = new IpGeolocationLocationProvider(httpClient, "https://fake.test/json");

        var location = await provider.GetCurrentLocationAsync();

        Assert.NotNull(location);
        Assert.True(location!.HasCoordinates);
        Assert.Equal(LocationSource.IpAddress, location.Source);
        Assert.Equal(4.7110, location.Latitude, 4);
        Assert.Equal(-74.0721, location.Longitude, 4);
        Assert.Equal("203.0.113.7", location.IpAddress);
    }

    private sealed class FakeLocationProvider : ICurrentLocationProvider
    {
        private readonly GeoLocation? _location;

        public FakeLocationProvider(GeoLocation? location)
        {
            _location = location;
        }

        public Task<GeoLocation?> GetCurrentLocationAsync(CancellationToken ct = default)
            => Task.FromResult(_location);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _payload;

        public StubHttpMessageHandler(string payload)
        {
            _payload = payload;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_payload, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }
}
