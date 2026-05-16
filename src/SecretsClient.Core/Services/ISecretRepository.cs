using SecretsClient.Core.Domain.Entities;
using SecretsClient.Core.Domain.ValueObjects;

namespace SecretsClient.Core.Services;

public interface ISecretRepository
{
    Task<Secret?> GetByIdAsync(SecretId id, CancellationToken ct = default);
    Task<IReadOnlyCollection<Secret>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyCollection<Secret>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Secret secret, CancellationToken ct = default);
    Task DeleteAsync(SecretId id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
