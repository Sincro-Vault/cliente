using SecretsClient.Core.Domain.Entities;
using SecretsClient.Core.Domain.ValueObjects;

namespace SecretsClient.Core.Services;

public interface IFragmentRepository
{
    Task<IReadOnlyCollection<SecretFragment>> GetBySecretIdAsync(SecretId secretId, CancellationToken ct = default);
    Task AddAsync(SecretFragment fragment, CancellationToken ct = default);
    Task DeleteBySecretIdAsync(SecretId secretId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
