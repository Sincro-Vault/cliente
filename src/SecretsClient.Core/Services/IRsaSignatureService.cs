using System.Security.Cryptography;

namespace SecretsClient.Core.Services;

public interface IRsaSignatureService
{
    /// <summary>
    /// Genera un nuevo par de claves RSA (2048 bits)
    /// </summary>
    RSA GenerateKeyPair();

    /// <summary>
    /// Firma datos con la clave privada RSA
    /// </summary>
    byte[] SignData(byte[] data, RSA privateKey);

    /// <summary>
    /// Verifica la firma de datos con la clave pública RSA
    /// </summary>
    bool VerifySignature(byte[] data, byte[] signature, RSA publicKey);

    /// <summary>
    /// Guarda la clave privada encriptada en archivo
    /// </summary>
    void SavePrivateKeyToFile(RSA key, string filePath);

    /// <summary>
    /// Carga la clave privada desde archivo encriptado
    /// </summary>
    RSA LoadPrivateKeyFromFile(string filePath);

    /// <summary>
    /// Exporta la clave pública en formato PEM
    /// </summary>
    string ExportPublicKeyPem(RSA key);

    /// <summary>
    /// Importa clave pública desde formato PEM
    /// </summary>
    RSA ImportPublicKeyPem(string pemData);
}
