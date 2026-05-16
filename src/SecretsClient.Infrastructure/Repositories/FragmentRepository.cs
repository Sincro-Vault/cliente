using Microsoft.EntityFrameworkCore;
using SecretsClient.Core.Domain.Entities;
using SecretsClient.Core.Domain.ValueObjects;
using SecretsClient.Core.Services;
using SecretsClient.Infrastructure.Data;

namespace SecretsClient.Infrastructure.Repositories;

public class FragmentRepository : IFragmentRepository
{
    private readonly SecretsDbContext _context;

    public FragmentRepository(SecretsDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<SecretFragment>> GetBySecretIdAsync(SecretId secretId, CancellationToken ct = default)
    {
        var list = await _context.Fragments
            .Where(f => f.SecretId == secretId)
            .ToListAsync(ct);
        return list.AsReadOnly();
    }

    public async Task AddAsync(SecretFragment fragment, CancellationToken ct = default)
        => await _context.Fragments.AddAsync(fragment, ct);

    public async Task DeleteBySecretIdAsync(SecretId secretId, CancellationToken ct = default)
    {
        var fragments = await _context.Fragments
            .Where(f => f.SecretId == secretId)
            .ToListAsync(ct);
        _context.Fragments.RemoveRange(fragments);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
