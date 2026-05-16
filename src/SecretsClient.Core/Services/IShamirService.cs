using SecretsClient.Core.Domain.ValueObjects;

namespace SecretsClient.Core.Services;

public interface IShamirService
{
    Task<IReadOnlyList<FragmentShare>> GenerateSharesAsync(
        byte[] secretBytes,
        int totalShares,
        int threshold,
        CancellationToken ct = default);

    Task<byte[]> ReconstructSecretAsync(
        IReadOnlyCollection<FragmentShare> shares,
        CancellationToken ct = default);
}

public record FragmentShare(int Index, byte[] EncryptedShare, byte[] Nonce);
