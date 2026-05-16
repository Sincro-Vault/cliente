using System.Security.Cryptography;
using SecretsClient.Core.Services;

namespace SecretsClient.Infrastructure.Crypto;

public sealed class CryptoService : ICryptoService
{
    private const int Aes256KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public Task<byte[]> EncryptAsync(byte[] plaintext, byte[] key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        ValidateKey(key);
        ct.ThrowIfCancellationRequested();

        var nonce = GenerateSecureRandom(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];
        var encryptedPayload = new byte[NonceSize + TagSize + ciphertext.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        Buffer.BlockCopy(nonce, 0, encryptedPayload, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, encryptedPayload, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, encryptedPayload, NonceSize + TagSize, ciphertext.Length);

        return Task.FromResult(encryptedPayload);
    }

    public Task<byte[]> DecryptAsync(byte[] ciphertextWithNonceTag, byte[] key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ciphertextWithNonceTag);
        ValidateKey(key);
        ct.ThrowIfCancellationRequested();

        if (ciphertextWithNonceTag.Length < NonceSize + TagSize)
            throw new ArgumentException("Encrypted payload is too short.", nameof(ciphertextWithNonceTag));

        var nonce = ciphertextWithNonceTag[..NonceSize];
        var tag = ciphertextWithNonceTag[NonceSize..(NonceSize + TagSize)];
        var ciphertext = ciphertextWithNonceTag[(NonceSize + TagSize)..];
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Task.FromResult(plaintext);
    }

    public byte[] DeriveKey(byte[] masterKey, byte[] salt, int iterations = 100_000)
    {
        ArgumentNullException.ThrowIfNull(masterKey);
        ArgumentNullException.ThrowIfNull(salt);

        if (masterKey.Length == 0)
            throw new ArgumentException("Master key must not be empty.", nameof(masterKey));

        if (salt.Length == 0)
            throw new ArgumentException("Salt must not be empty.", nameof(salt));

        if (iterations <= 0)
            throw new ArgumentOutOfRangeException(nameof(iterations), "Iterations must be greater than zero.");

        return Rfc2898DeriveBytes.Pbkdf2(masterKey, salt, iterations, HashAlgorithmName.SHA256, Aes256KeySize);
    }

    public byte[] GenerateSecureRandom(int length = 32)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Random buffer length must be greater than zero.");

        var buffer = new byte[length];
        RandomNumberGenerator.Fill(buffer);
        return buffer;
    }

    public string ComputeChecksum(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant();
    }

    public byte[] ZeroMemory(byte[] buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        CryptographicOperations.ZeroMemory(buffer);
        return buffer;
    }

    private static void ValidateKey(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (key.Length != Aes256KeySize)
            throw new ArgumentException("AES-256-GCM requires a 32-byte key.", nameof(key));
    }
}
