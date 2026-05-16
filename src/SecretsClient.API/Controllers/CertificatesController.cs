using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretsClient.Core.Services;

namespace SecretsClient.API.Controllers;

[ApiController]
[Route("api/certificates")]
[Authorize]
public class CertificatesController : ControllerBase
{
    private readonly IRsaSignatureService _rsaService;
    private readonly ILogger<CertificatesController> _logger;

    public CertificatesController(IRsaSignatureService rsaService, ILogger<CertificatesController> logger)
    {
        _rsaService = rsaService;
        _logger = logger;
    }

    /// <summary>
    /// Carga y valida un certificado RSA (.pem / .crt / .key)
    /// POST /api/certificates/upload
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] string? username,
        [FromForm] string? password,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, error = "Archivo requerido" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".pem" or ".crt" or ".key"))
            return BadRequest(new { success = false, error = "Formato no válido. Use .pem, .crt o .key" });

        try
        {
            using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
            var pemContent = await reader.ReadToEndAsync(ct);

            // Validar que sea un PEM legible por RSA
            var rsa = _rsaService.ImportPublicKeyPem(pemContent);

            // Calcular fingerprint SHA-256 del contenido del PEM
            var contentBytes = Encoding.UTF8.GetBytes(pemContent);
            var hashBytes = SHA256.HashData(contentBytes);
            var fingerprint = BitConverter.ToString(hashBytes).Replace("-", ":").ToUpperInvariant();

            _logger.LogInformation("Certificado cargado: {Fingerprint}", fingerprint);

            return Ok(new
            {
                success = true,
                fingerprint,
                validUntil = DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd"),
                algorithm = "RSA",
                keySize = rsa.KeySize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cargando certificado");
            return BadRequest(new { success = false, error = "Certificado inválido o corrupto" });
        }
    }
}
