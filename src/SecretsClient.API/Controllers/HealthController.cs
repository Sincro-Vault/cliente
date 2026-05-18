using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretsClient.Core.Services;

namespace SecretsClient.API.Controllers;

[ApiController]
[Route("api/health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly IHealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IHealthCheckService healthCheckService, ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    /// <summary>
    /// Verifica el estado general del servicio
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> Health(CancellationToken ct)
    {
        try
        {
            var status = await _healthCheckService.CheckHealthAsync(ct);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en health check");
            return StatusCode(503, new { status = "unhealthy", error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene estadísticas de almacenamiento
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        try
        {
            var stats = await _healthCheckService.GetStorageStatsAsync(ct);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo estadísticas");
            return StatusCode(500, ex.Message);
        }
    }

    /// <summary>
    /// Retorna la versión del servicio
    /// </summary>
    [HttpGet("version")]
    [ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK)]
    public IActionResult GetVersion()
    {
        return Ok(new
        {
            version = "1.0.0",
            name = "SecretsClient",
            build_date = "2024-05-14"
        });
    }

    /// <summary>
    /// Simple ping para verificar que el servicio está activo
    /// </summary>
    [HttpGet("ping")]
    [ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK)]
    public IActionResult Ping()
    {
        return Ok(new { pong = DateTime.UtcNow });
    }
}
