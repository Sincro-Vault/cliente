using Microsoft.EntityFrameworkCore;
using SecretsClient.Core.Domain.Entities;
using SecretsClient.Core.Services;
using SecretsClient.Infrastructure.Data;

namespace SecretsClient.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly SecretsDbContext _context;

    public UserRepository(SecretsDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Users.FindAsync(new object[] { id }, ct);

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => await _context.Users.FirstOrDefaultAsync(u => u.Username == username, ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
        => await _context.Users.AddAsync(user, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
