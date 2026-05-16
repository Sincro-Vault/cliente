using Microsoft.EntityFrameworkCore;
using SecretsClient.Core.Domain.Entities;
using SecretsClient.Core.Domain.ValueObjects;
using SecretsClient.Core.Services;
using SecretsClient.Infrastructure.Data;

namespace SecretsClient.Infrastructure.Repositories;

public class SecretRepository : ISecretRepository
{
    private readonly SecretsDbContext _context;

    public SecretRepository(SecretsDbContext context)
    {
        _context = context;
    }

    public async Task<Secret?> GetByIdAsync(SecretId id, CancellationToken ct = default)
        => await _context.Secrets
            .Include(s => s.Fragments)
            .Include(s => s.GeoPolicies)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyCollection<Secret>> GetAllByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var list = await _context.Secrets
            .Include(s => s.Fragments)
            .Where(s => s.UserId == userId)
            .ToListAsync(ct);
        return list.AsReadOnly();
    }

    public async Task<IReadOnlyCollection<Secret>> GetAllAsync(CancellationToken ct = default)
    {
        var list = await _context.Secrets
            .Include(s => s.Fragments)
            .ToListAsync(ct);
        return list.AsReadOnly();
    }

    public async Task AddAsync(Secret secret, CancellationToken ct = default)
        => await _context.Secrets.AddAsync(secret, ct);

    public async Task DeleteAsync(SecretId id, CancellationToken ct = default)
    {
        var secret = await _context.Secrets.FindAsync(new object[] { id }, ct);
        if (secret != null)
            _context.Secrets.Remove(secret);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
