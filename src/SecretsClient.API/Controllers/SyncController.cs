using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SecretsClient.Core.Services;
using SecretsClient.Core.DTOs;
using SecretsClient.Core.Domain.ValueObjects;

namespace SecretsClient.API.Controllers;

[ApiController]
[Route("api/sync")]
[Authorize]
public class SyncController : ControllerBase
{
    private readonly ISyncService _syncService;
    private readonly ILogger<SyncController> _logger;

    public SyncController(ISyncService syncService, ILogger<SyncController> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    [HttpPost("metadata")]
    public async Task<IActionResult> SyncMetadata(CancellationToken ct)
    {
        await _syncService.SyncMetadataAsync(ct);
        return Ok(new SyncStatusResult(true, null));
    }

    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat(CancellationToken ct)
    {
        await _syncService.HeartbeatAsync(ct);
        return NoContent();
    }
}
