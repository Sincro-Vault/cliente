using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecretsClient.Core.Domain.ValueObjects;
using SecretsClient.Core.Services;

namespace SecretsClient.Infrastructure.Sync;

/// <summary>
/// Cliente HTTP que habla con los endpoints internos del servidor Python.
/// </summary>
public class FragmentSyncClient : IFragmentSyncClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FragmentSyncClient> _logger;
    private readonly string _internalToken;

    public FragmentSyncClient(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<FragmentSyncClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _internalToken = config["Server:InternalToken"] ?? "shared-secret-cliente-servidor-2026";

        if (!_httpClient.DefaultRequestHeaders.Contains("X-Internal-Token"))
            _httpClient.DefaultRequestHeaders.Add("X-Internal-Token", _internalToken);
    }

    public async Task UploadFragmentAsync(
        Guid userId,
        string username,
        SecretId secretId,
        int fragmentIndex,
        string encryptedFragmentBase64,
        string checksum,
        CancellationToken ct = default)
    {
        var payload = new
        {
            user_id = userId.ToString().ToUpperInvariant(),
            username,
            secret_id = secretId.Value.ToString().ToUpperInvariant(),
            fragment_index = fragmentIndex,
            encrypted_fragment_b64 = encryptedFragmentBase64,
            checksum
        };

        var response = await _httpClient.PostAsJsonAsync("/api/internal/fragments", payload, ct);
        response.EnsureSuccessStatusCode();
        _logger.LogInformation("Fragmento F{Index} del secreto {SecretId} enviado al servidor", fragmentIndex, secretId);
    }

    public async Task<string?> DownloadFragmentAsync(
        Guid userId,
        SecretId secretId,
        CancellationToken ct = default)
    {
        var url = $"/api/internal/fragments/{userId.ToString().ToUpperInvariant()}/{secretId.Value.ToString().ToUpperInvariant()}";
        var response = await _httpClient.GetAsync(url, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<RemoteFragmentResult>(cancellationToken: ct);
        return result?.encrypted_fragment_b64;
    }

    public async Task DeleteFragmentAsync(
        Guid userId,
        SecretId secretId,
        CancellationToken ct = default)
    {
        var url = $"/api/internal/fragments/{userId.ToString().ToUpperInvariant()}/{secretId.Value.ToString().ToUpperInvariant()}";
        var response = await _httpClient.DeleteAsync(url, ct);
        if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
            response.EnsureSuccessStatusCode();
    }

    private record RemoteFragmentResult(bool success, int fragment_index, string encrypted_fragment_b64, string checksum, string ledger_block_hash);
}
