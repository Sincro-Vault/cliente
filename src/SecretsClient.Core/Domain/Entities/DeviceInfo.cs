namespace SecretsClient.Core.Domain.Entities;

public sealed class DeviceInfo
{
    public string DeviceId { get; private set; } = string.Empty;
    public string DeviceName { get; private set; } = string.Empty;
    public DateTime LastSync { get; private set; }
    public string? PublicKey { get; private set; }

    private DeviceInfo() { }

    public static DeviceInfo Create(string deviceId, string deviceName)
    {
        return new DeviceInfo
        {
            DeviceId = deviceId,
            DeviceName = deviceName,
            LastSync = DateTime.MinValue,
            PublicKey = null
        };
    }

    public void UpdateSyncTime() => LastSync = DateTime.UtcNow;
}
