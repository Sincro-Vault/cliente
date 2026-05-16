using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SecretsClient.Core.Services;
using SecretsClient.Core.DTOs;

namespace SecretsClient.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Registra un nuevo usuario en el cliente
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("Username, Password y Email son requeridos");
            }

            var result = await _authService.RegisterAsync(request, ct);
            return CreatedAtAction(nameof(Register), result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en Register");
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Autentica un usuario y retorna JWT
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Username y Password son requeridos");
            }

            var response = await _authService.LoginAsync(request, ct);

            // Extraer info del JWT para devolver el formato que espera el frontend
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadToken(response.AccessToken) as JwtSecurityToken;
            var userId = jwt?.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? "";
            var username = jwt?.Claims.FirstOrDefault(c => c.Type == "username")?.Value ?? request.Username;

            return Ok(new
            {
                user = new { id = userId, username, email = "", company = "", role = "user" },
                token = response.AccessToken,
                refreshToken = response.RefreshToken,
                expiresInSeconds = response.ExpiresInSeconds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en Login");
            return Unauthorized(ex.Message);
        }
    }

    /// <summary>
    /// Refresca el token de acceso usando el refresh token
    /// </summary>
    [HttpPost("refresh")]
    [Authorize]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest("RefreshToken es requerido");
            }

            var response = await _authService.RefreshTokenAsync(request, ct);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en RefreshToken");
            return Unauthorized(ex.Message);
        }
    }

    /// <summary>
    /// Cierra la sesión del usuario actual
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("No se pudo obtener el ID del usuario");
            }

            await _authService.LogoutAsync(userId, ct);
            return Ok(new { message = "Sesión cerrada exitosamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en Logout");
            return StatusCode(500, ex.Message);
        }
    }
}
