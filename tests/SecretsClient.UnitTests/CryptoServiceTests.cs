using System.Security.Cryptography;
using SecretsClient.Infrastructure.Crypto;

namespace SecretsClient.UnitTests;

public sealed class CryptoServiceTests
{
    private readonly CryptoService _cryptoService = new();

    [Fact]
    public async Task EncryptAndDecryptAsync_RoundTrip_ReturnsOriginalPlaintext()
    {
        var key = _cryptoService.GenerateSecureRandom(32);
        var plaintext = "cliente-crypto-fragmento"u8.ToArray();

        var encrypted = await _cryptoService.EncryptAsync(plaintext, key);
        var decrypted = await _cryptoService.DecryptAsync(encrypted, key);

        Assert.NotEqual(plaintext, encrypted);
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public async Task DecryptAsync_WithWrongKey_ThrowsCryptographicException()
    {
        var key = _cryptoService.GenerateSecureRandom(32);
        var wrongKey = _cryptoService.GenerateSecureRandom(32);
        var plaintext = "fragmento-sensible"u8.ToArray();
        var encrypted = await _cryptoService.EncryptAsync(plaintext, key);

        await Assert.ThrowsAnyAsync<CryptographicException>(() => _cryptoService.DecryptAsync(encrypted, wrongKey));
    }

    [Fact]
    public void ZeroMemory_ClearsBuffer()
    {
        var buffer = new byte[] { 1, 2, 3, 4 };

        _cryptoService.ZeroMemory(buffer);

        Assert.All(buffer, value => Assert.Equal(0, value));
    }
}
