using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SecretsClient.Core.Domain.Entities;
using SecretsClient.Core.Services;
using SecretsClient.Core.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SecretsClient.Infrastructure.Auth;

public class AuthService : IAuthService
{
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;
    private readonly IUserRepository _userRepository;
    private readonly IRsaSignatureService _rsaService;
    private readonly Dictionary<string, string> _revokedTokens = new(); // En producción, usar Redis

    public AuthService(
        IConfiguration config,
        ILogger<AuthService> logger,
        IUserRepository userRepository,
        IRsaSignatureService rsaService)
    {
        _config = config;
        _logger = logger;
        _userRepository = userRepository;
        _rsaService = rsaService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        try
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username, ct);
            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Credenciales incorrectas");

            var userId = user.Id.ToString();
            var accessToken = GenerateJwtToken(userId, request.Username);
            var refreshToken = GenerateRefreshToken();

            _logger.LogInformation("Usuario {Username} logueado exitosamente", request.Username);

            return new LoginResponse(
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                ExpiresInSeconds: 600,
                TokenType: "Bearer");
        }
        catch (Exception ex) when (ex is not UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Error en Login para usuario {Username}", request.Username);
            throw;
        }
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshRequest request, CancellationToken ct)
    {
        try
        {
            // TODO: Validar refreshToken en BD (requiere tabla de refresh tokens)
            var userId = Guid.NewGuid().ToString();
            var accessToken = GenerateJwtToken(userId, "user");

            return await Task.FromResult(new LoginResponse(
                AccessToken: accessToken,
                RefreshToken: GenerateRefreshToken(),
                ExpiresInSeconds: 3600));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en RefreshToken");
            throw;
        }
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        try
        {
            var existing = await _userRepository.GetByUsernameAsync(request.Username, ct);
            if (existing != null)
                throw new InvalidOperationException($"El usuario '{request.Username}' ya existe");

            var passwordHash = HashPassword(request.Password);

            string publicKey;
            if (request.CertificatePem != null && request.CertificatePem.Length > 0)
            {
                publicKey = Encoding.UTF8.GetString(request.CertificatePem);
            }
            else
            {
                var rsa = _rsaService.GenerateKeyPair();
                publicKey = _rsaService.ExportPublicKeyPem(rsa);
            }

            var user = User.Create(request.Username, passwordHash, publicKey);
            await _userRepository.AddAsync(user, ct);
            await _userRepository.SaveChangesAsync(ct);

            _logger.LogInformation("Usuario {Username} registrado exitosamente", request.Username);

            return new RegisterResponse(
                UserId: user.Id.ToString(),
                Username: user.Username,
                Email: request.Email,
                CreatedAt: user.CreatedAt);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Error en Register para usuario {Username}", request.Username);
            throw;
        }
    }

    public async Task LogoutAsync(string userId, CancellationToken ct)
    {
        try
        {
            // TODO: Revocar tokens en BD
            _revokedTokens[userId] = DateTime.UtcNow.ToString("O");
            _logger.LogInformation("Usuario {UserId} deslogueado", userId);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en Logout");
            throw;
        }
    }

    public async Task<bool> ValidateTokenAsync(string token, CancellationToken ct)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

            if (jwtToken == null) return false;

            var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            if (string.IsNullOrEmpty(userId)) return false;

            if (_revokedTokens.ContainsKey(userId)) return false;

            return await Task.FromResult(true);
        }
        catch
        {
            return false;
        }
    }

    private string GenerateJwtToken(string userId, string username)
    {
        var jwtSettings = _config.GetSection("Jwt");
        var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey no configurado"));

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(secretKey),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("sub", userId),
            new Claim("username", username),
            new Claim("device_id", _config["Client:DeviceId"] ?? "unknown"),
            new Claim(ClaimTypes.NameIdentifier, userId)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
        }
        return Convert.ToBase64String(randomNumber);
    }

    private static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var hash = pbkdf2.GetBytes(32);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var expectedHash = Convert.FromBase64String(parts[1]);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
        var actualHash = pbkdf2.GetBytes(32);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
