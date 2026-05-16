using System.Text;
using Newtonsoft.Json;
using SecretsClient.Core.Services;
using SecretsClient.Core.Domain.Entities;
using SecretsClient.Core.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SecretsClient.Infrastructure.Sync;

public class ServerSyncService : ISyncService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<ServerSyncService> _logger;

    public ServerSyncService(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<ServerSyncService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task SyncMetadataAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Sincronizando metadata...");
        await Task.CompletedTask;
    }

    public async Task UploadFragmentMetadataAsync(SecretFragment fragment, CancellationToken ct = default)
    {
        _logger.LogInformation("Subiendo metadata del fragmento {FragmentId}", fragment.Id);
        await Task.CompletedTask;
    }

    public async Task<IReadOnlyList<RemoteFragmentInfo>> DownloadRemoteFragmentsAsync(SecretId secretId, CancellationToken ct = default)
    {
        _logger.LogInformation("Descargando fragmentos remotos para {SecretId}", secretId);
        return await Task.FromResult(new List<RemoteFragmentInfo>().AsReadOnly());
    }

    public async Task HeartbeatAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Enviando heartbeat...");
        await Task.CompletedTask;
    }
}
