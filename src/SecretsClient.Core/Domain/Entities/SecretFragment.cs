using SecretsClient.Core.Domain.ValueObjects;

namespace SecretsClient.Core.Domain.Entities;

public sealed class SecretFragment
{
    public Guid Id { get; private set; }
    public SecretId SecretId { get; private set; } = null!;
    public int FragmentIndex { get; private set; }
    public string EncryptedFragment { get; private set; } = string.Empty;
    public string FragmentChecksum { get; private set; } = string.Empty;
    public string StorageLocation { get; private set; } = "local";
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    private SecretFragment() { }

    public static SecretFragment Create(
        SecretId secretId,
        int index,
        string encryptedFragmentBase64,
        string checksum,
        string location = "local",
        DateTime? expires = null)
    {
        return new SecretFragment
        {
            Id = Guid.NewGuid(),
            SecretId = secretId,
            FragmentIndex = index,
            EncryptedFragment = encryptedFragmentBase64,
            FragmentChecksum = checksum,
            StorageLocation = location,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expires
        };
    }
}
