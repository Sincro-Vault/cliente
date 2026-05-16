using SecretsClient.Core.Services;
using SecretsClient.Infrastructure.Shamir;

namespace SecretsClient.UnitTests;

public sealed class ShamirServiceTests
{
    private readonly ShamirService _shamirService = new();

    [Fact]
    public async Task GenerateSharesAndReconstructSecret_WithThresholdSubset_ReturnsOriginalSecret()
    {
        var secret = "secreto-umbral-3-de-5"u8.ToArray();
        var shares = await _shamirService.GenerateSharesAsync(secret, totalShares: 5, threshold: 3);
        var reconstructionSet = new[] { shares[0], shares[2], shares[4] };

        var reconstructed = await _shamirService.ReconstructSecretAsync(reconstructionSet);

        Assert.Equal(secret, reconstructed);
    }

    [Fact]
    public async Task ReconstructSecretAsync_WithFewerThanThresholdShares_Throws()
    {
        var secret = "fragmentacion"u8.ToArray();
        var shares = await _shamirService.GenerateSharesAsync(secret, totalShares: 5, threshold: 3);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _shamirService.ReconstructSecretAsync(new[] { shares[0], shares[1] }));

        Assert.Contains("At least 3 shares", exception.Message);
    }

    [Fact]
    public async Task ReconstructSecretAsync_WithMismatchedShareIndexMetadata_Throws()
    {
        var secret = "tamper-detection"u8.ToArray();
        var shares = await _shamirService.GenerateSharesAsync(secret, totalShares: 3, threshold: 2);
        var tamperedPayload = shares[0].EncryptedShare.ToArray();
        tamperedPayload[1] = 99;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _shamirService.ReconstructSecretAsync(
                new[]
                {
                    new FragmentShare(shares[0].Index, tamperedPayload, Array.Empty<byte>()),
                    shares[1]
                }));

        Assert.Contains("index metadata", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
