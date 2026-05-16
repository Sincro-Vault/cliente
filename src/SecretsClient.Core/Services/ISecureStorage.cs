using SecretsClient.Core.Domain.Entities;
using SecretsClient.Core.Domain.ValueObjects;

namespace SecretsClient.Core.Services;

public interface ISecureStorage
{
    Task InitializeAsync(CancellationToken ct = default);
    Task<Secret?> GetSecretAsync(SecretId id, CancellationToken ct = default);
    Task<IReadOnlyCollection<Secret>> GetAllSecretsAsync(CancellationToken ct = default);
    Task SaveSecretAsync(Secret secret, CancellationToken ct = default);
    Task DeleteSecretAsync(SecretId id, CancellationToken ct = default);
    Task<IReadOnlyCollection<SecretFragment>> GetFragmentsAsync(SecretId id, CancellationToken ct = default);
    Task SaveFragmentAsync(SecretFragment fragment, CancellationToken ct = default);
    Task DeleteFragmentsAsync(SecretId secretId, CancellationToken ct = default);
}
