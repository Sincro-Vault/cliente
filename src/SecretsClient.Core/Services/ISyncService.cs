using SecretsClient.Core.Domain.Entities;
using SecretsClient.Core.Domain.ValueObjects;

namespace SecretsClient.Core.Services;

public interface ISyncService
{
    Task SyncMetadataAsync(CancellationToken ct = default);
    Task UploadFragmentMetadataAsync(SecretFragment fragment, CancellationToken ct = default);
    Task<IReadOnlyList<RemoteFragmentInfo>> DownloadRemoteFragmentsAsync(SecretId secretId, CancellationToken ct = default);
    Task HeartbeatAsync(CancellationToken ct = default);
}

public record RemoteFragmentInfo(Guid FragmentId, string ServerId, string LocationHint);
