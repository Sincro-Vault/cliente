using System;
using SecretsClient.Core.Domain.ValueObjects;

namespace SecretsClient.Core.Domain.Entities;

public sealed class GeoPolicy
{
    public Guid Id { get; private set; }
    public SecretId SecretId { get; private set; } = null!;
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public double RadiusMeters { get; private set; }
    public string Description { get; private set; } = string.Empty;

    private GeoPolicy() { }

    public static GeoPolicy Create(SecretId secretId, double latitude, double longitude, double radiusMeters, string description)
    {
        return new GeoPolicy
        {
            Id = Guid.NewGuid(),
            SecretId = secretId,
            Latitude = latitude,
            Longitude = longitude,
            RadiusMeters = radiusMeters,
            Description = description
        };
    }
}
