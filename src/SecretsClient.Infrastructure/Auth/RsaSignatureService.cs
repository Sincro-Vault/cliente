using System.Security.Cryptography;
using System.Text;
using SecretsClient.Core.Services;
using Microsoft.Extensions.Logging;

namespace SecretsClient.Infrastructure.Auth;

/// <summary>
/// Servicio de firmas RSA para comunicación segura con servidor central
/// </summary>
public class RsaSignatureService : IRsaSignatureService
{
    private readonly ILogger<RsaSignatureService> _logger;

    public RsaSignatureService(ILogger<RsaSignatureService> logger)
    {
        _logger = logger;
    }

    public RSA GenerateKeyPair()
    {
        try
        {
            var rsa = RSA.Create();
            rsa.KeySize = 2048;
            _logger.LogInformation("Par de claves RSA generado (2048 bits)");
            return rsa;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando par de claves RSA");
            throw;
        }
    }

    public byte[] SignData(byte[] data, RSA privateKey)
    {
        try
        {
            return privateKey.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error firmando datos");
            throw;
        }
    }

    public bool VerifySignature(byte[] data, byte[] signature, RSA publicKey)
    {
        try
        {
            return publicKey.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verificando firma");
            return false;
        }
    }

    public void SavePrivateKeyToFile(RSA key, string filePath)
    {
        try
        {
            var pem = key.ExportRSAPrivateKeyPem();
            var encrypted = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(pem),
                null,
                DataProtectionScope.CurrentUser);
            
            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
            File.WriteAllBytes(filePath, encrypted);
            _logger.LogInformation("Clave privada RSA guardada encriptada en {Path}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando clave privada RSA");
            throw;
        }
    }

    public RSA LoadPrivateKeyFromFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Archivo de clave privada no encontrado: {filePath}");

            var encrypted = File.ReadAllBytes(filePath);
            var pem = Encoding.UTF8.GetString(ProtectedData.Unprotect(
                encrypted,
                null,
                DataProtectionScope.CurrentUser));

            var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            _logger.LogInformation("Clave privada RSA cargada desde {Path}", filePath);
            return rsa;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando clave privada RSA");
            throw;
        }
    }

    public string ExportPublicKeyPem(RSA key)
    {
        try
        {
            return key.ExportRSAPublicKeyPem();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exportando clave pública RSA");
            throw;
        }
    }

    public RSA ImportPublicKeyPem(string pemData)
    {
        try
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(pemData);
            return rsa;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importando clave pública RSA");
            throw;
        }
    }
}
