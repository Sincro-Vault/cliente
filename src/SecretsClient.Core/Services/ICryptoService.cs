namespace SecretsClient.Core.Services;

public interface ICryptoService
{
    Task<byte[]> EncryptAsync(byte[] plaintext, byte[] key, CancellationToken ct = default);
    Task<byte[]> DecryptAsync(byte[] ciphertextWithNonceTag, byte[] key, CancellationToken ct = default);
    byte[] DeriveKey(byte[] masterKey, byte[] salt, int iterations = 100_000);
    byte[] GenerateSecureRandom(int length = 32);
    string ComputeChecksum(byte[] data);
    byte[] ZeroMemory(byte[] buffer);
}
