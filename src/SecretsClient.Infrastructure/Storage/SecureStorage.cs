using SecretsClient.Core.Domain.Entities;
using SecretsClient.Core.Domain.ValueObjects;
using SecretsClient.Core.Services;

namespace SecretsClient.Infrastructure.Storage;

public class SecureStorage : ISecureStorage
{
    private readonly ISecretRepository _secretRepo;
    private readonly IFragmentRepository _fragmentRepo;

    public SecureStorage(ISecretRepository secretRepo, IFragmentRepository fragmentRepo)
    {
        _secretRepo = secretRepo;
        _fragmentRepo = fragmentRepo;
    }

    public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task<Secret?> GetSecretAsync(SecretId id, CancellationToken ct = default)
        => _secretRepo.GetByIdAsync(id, ct);

    public Task<IReadOnlyCollection<Secret>> GetAllSecretsAsync(CancellationToken ct = default)
        => _secretRepo.GetAllAsync(ct);

    public async Task SaveSecretAsync(Secret secret, CancellationToken ct = default)
    {
        await _secretRepo.AddAsync(secret, ct);
        await _secretRepo.SaveChangesAsync(ct);
    }

    public async Task DeleteSecretAsync(SecretId id, CancellationToken ct = default)
    {
        await _fragmentRepo.DeleteBySecretIdAsync(id, ct);
        await _fragmentRepo.SaveChangesAsync(ct);
        await _secretRepo.DeleteAsync(id, ct);
        await _secretRepo.SaveChangesAsync(ct);
    }

    public Task<IReadOnlyCollection<SecretFragment>> GetFragmentsAsync(SecretId id, CancellationToken ct = default)
        => _fragmentRepo.GetBySecretIdAsync(id, ct);

    public async Task SaveFragmentAsync(SecretFragment fragment, CancellationToken ct = default)
    {
        await _fragmentRepo.AddAsync(fragment, ct);
        await _fragmentRepo.SaveChangesAsync(ct);
    }

    public async Task DeleteFragmentsAsync(SecretId secretId, CancellationToken ct = default)
    {
        await _fragmentRepo.DeleteBySecretIdAsync(secretId, ct);
        await _fragmentRepo.SaveChangesAsync(ct);
    }
}
