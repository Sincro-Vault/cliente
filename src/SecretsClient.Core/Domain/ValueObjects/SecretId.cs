namespace SecretsClient.Core.Domain.ValueObjects;

public sealed record SecretId(Guid Value)
{
    public static SecretId New() => new(Guid.NewGuid());
    public static SecretId From(Guid guid) => new(guid);
    public static SecretId From(string guid) => new(Guid.Parse(guid));
    public override string ToString() => Value.ToString();
}
