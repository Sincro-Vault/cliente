using SecretsClient.Core.Services;
using SecretsClient.Infrastructure.Crypto;
using SecretsClient.Infrastructure.Shamir;

namespace SecretsClient.IntegrationTests;

public sealed class FragmentProtectionWorkflowTests
{
    [Fact]
    public async Task ShamirAndCryptoServices_CanProtectAndRecoverSecret()
    {
        IShamirService shamirService = new ShamirService();
        ICryptoService cryptoService = new CryptoService();

        var secret = "flujo-criptografico-completo"u8.ToArray();
        var masterKey = cryptoService.GenerateSecureRandom(32);
        var salt = cryptoService.GenerateSecureRandom(16);
        var fragmentKey = cryptoService.DeriveKey(masterKey, salt);

        var shares = await shamirService.GenerateSharesAsync(secret, totalShares: 5, threshold: 3);
        var protectedShares = new List<FragmentShare>(shares.Count);

        foreach (var share in shares)
        {
            var encryptedShare = await cryptoService.EncryptAsync(share.EncryptedShare, fragmentKey);
            protectedShares.Add(new FragmentShare(share.Index, encryptedShare, Array.Empty<byte>()));
        }

        var decryptedShares = new List<FragmentShare>(3);

        foreach (var share in protectedShares.Take(3))
        {
            var decryptedShare = await cryptoService.DecryptAsync(share.EncryptedShare, fragmentKey);
            decryptedShares.Add(new FragmentShare(share.Index, decryptedShare, Array.Empty<byte>()));
        }

        var reconstructed = await shamirService.ReconstructSecretAsync(decryptedShares);

        Assert.Equal(secret, reconstructed);
    }
}
