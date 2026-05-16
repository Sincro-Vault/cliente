using System;
using System.Collections.Generic;

namespace SecretsClient.Core.Domain.Entities;

public sealed class User
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string PublicKey { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private readonly List<Secret> _secrets = new();
    public IReadOnlyCollection<Secret> Secrets => _secrets.AsReadOnly();

    private User() { }

    public static User Create(string username, string passwordHash, string publicKey)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = passwordHash,
            PublicKey = publicKey,
            CreatedAt = DateTime.UtcNow
        };
    }
}
