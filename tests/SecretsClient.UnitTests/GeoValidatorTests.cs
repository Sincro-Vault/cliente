using SecretsClient.Core.Domain.Entities;
using SecretsClient.Core.Services;
using SecretsClient.Infrastructure.Geo;

namespace SecretsClient.UnitTests;

public sealed class GeoValidatorTests
{
    private readonly GeoValidator _validator = new();

    [Fact]
    public async Task ValidateLocationAsync_WhenInsideBoundary_ReturnsValid()
    {
        var current = GeoLocation.FromGps(4.7110, -74.0721, 5);
        var boundaries = new[] { new GeoBoundary(4.7110, -74.0721, 100) };

        var result = await _validator.ValidateLocationAsync(current, boundaries);

        Assert.True(result.IsValid);
        Assert.NotNull(result.DistanceMeters);
        Assert.True(result.DistanceMeters <= 100);
    }

    [Fact]
    public async Task ValidateLocationAsync_WhenOutsideBoundary_ReturnsInvalid()
    {
        var current = GeoLocation.FromGps(4.7110, -74.0721, 5);
        var boundaries = new[] { new GeoBoundary(6.2518, -75.5636, 100) };

        var result = await _validator.ValidateLocationAsync(current, boundaries);

        Assert.False(result.IsValid);
        Assert.NotNull(result.DistanceMeters);
        Assert.Contains("outside", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateLocationAsync_WithoutCoordinates_ReturnsInvalid()
    {
        var current = GeoLocation.FromIP("1.2.3.4");
        var boundaries = new[] { new GeoBoundary(4.7110, -74.0721, 100) };

        var result = await _validator.ValidateLocationAsync(current, boundaries);

        Assert.False(result.IsValid);
        Assert.Null(result.DistanceMeters);
    }

    [Fact]
    public async Task ValidateLocationAsync_WhenAccuracyMarginExceedsBoundary_ReturnsInvalid()
    {
        var current = GeoLocation.FromGps(4.7110, -74.0721, 150);
        var boundaries = new[] { new GeoBoundary(4.7110, -74.0721, 100) };

        var result = await _validator.ValidateLocationAsync(current, boundaries);

        Assert.False(result.IsValid);
        Assert.Contains("accuracy", result.Reason, StringComparison.OrdinalIgnoreCase);
    }
}
