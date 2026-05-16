using SecretsClient.Core.Domain.ValueObjects;

namespace SecretsClient.Core.Domain.Entities;

public sealed class Secret
{
    public SecretId Id { get; private set; } = SecretId.From(Guid.Empty);
    public string Name { get; private set; } = string.Empty;
    public string EncryptedPayload { get; private set; } = string.Empty;
    public string EncryptionAlgorithm { get; private set; } = "AES-256-GCM";
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string OwnerId { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    
    private readonly List<SecretFragment> _fragments = new();
    public IReadOnlyCollection<SecretFragment> Fragments => _fragments.AsReadOnly();

    private readonly List<GeoPolicy> _geoPolicies = new();
    public IReadOnlyCollection<GeoPolicy> GeoPolicies => _geoPolicies.AsReadOnly();

    private Secret() { }

    public static Secret Create(string name, byte[] plaintext, string ownerId, Guid userId)
    {
        return new Secret
        {
            Id = SecretId.New(),
            Name = name,
            EncryptedPayload = Convert.ToBase64String(plaintext),
            EncryptionAlgorithm = "AES-256-GCM",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            OwnerId = ownerId,
            UserId = userId
        };
    }

    public void AddFragment(SecretFragment fragment)
    {
        if (_fragments.Any(f => f.FragmentIndex == fragment.FragmentIndex))
            throw new InvalidOperationException($"Fragmento con índice {fragment.FragmentIndex} ya existe.");
        _fragments.Add(fragment);
    }

    public void RemoveFragment(SecretFragment fragment) => _fragments.Remove(fragment);
    public void UpdateName(string name) => Name = name;
    public void UpdateTimestamp() => UpdatedAt = DateTime.UtcNow;
    
    public void AddGeoPolicy(GeoPolicy policy) => _geoPolicies.Add(policy);
}
