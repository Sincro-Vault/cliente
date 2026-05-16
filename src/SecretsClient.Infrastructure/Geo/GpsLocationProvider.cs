using System.ComponentModel;
using System.Globalization;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using SecretsClient.Core.Domain.Entities;
using SecretsClient.Core.Services;

namespace SecretsClient.Infrastructure.Geo;

public sealed class GpsLocationProvider : ICurrentLocationProvider
{
    private readonly TimeSpan _timeout;

    public GpsLocationProvider(TimeSpan? timeout = null)
    {
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
    }

    public async Task<GeoLocation?> GetCurrentLocationAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
            return null;

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -EncodedCommand {BuildEncodedCommand()}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        try
        {
            if (!process.Start())
                return null;

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(_timeout + TimeSpan.FromSeconds(1));

            await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);

            var output = (await outputTask.ConfigureAwait(false)).Trim();
            _ = await errorTask.ConfigureAwait(false);

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
                return null;

            using var document = JsonDocument.Parse(output);
            var root = document.RootElement;

            if (!TryReadDouble(root, "latitude", out var latitude) ||
                !TryReadDouble(root, "longitude", out var longitude))
            {
                return null;
            }

            var accuracy = TryReadDouble(root, "accuracy", out var parsedAccuracy) ? parsedAccuracy : 0d;
            return GeoLocation.FromGps(latitude, longitude, accuracy);
        }
        catch (OperationCanceledException)
        {
            TryTerminateProcess(process);
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (Win32Exception)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private string BuildEncodedCommand()
    {
        var timeoutMs = Math.Max(1000, (int)_timeout.TotalMilliseconds);
        var script = $@"
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Runtime.WindowsRuntime
[Windows.Devices.Geolocation.Geolocator, Windows, ContentType=WindowsRuntime] > $null
$locator = [Windows.Devices.Geolocation.Geolocator]::new()
$locator.DesiredAccuracyInMeters = 50
$operation = $locator.GetGeopositionAsync()
$task = [System.WindowsRuntimeSystemExtensions]::AsTask($operation)
if (-not $task.Wait({timeoutMs})) {{ exit 2 }}
$position = $task.Result
$coordinates = $position.Coordinate.Point.Position
[pscustomobject]@{{
  latitude = $coordinates.Latitude
  longitude = $coordinates.Longitude
  accuracy = $position.Coordinate.Accuracy
}} | ConvertTo-Json -Compress";

        return Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
    }

    private static bool TryReadDouble(JsonElement root, string propertyName, out double value)
    {
        value = default;

        if (!root.TryGetProperty(propertyName, out var property))
            return false;

        if (property.ValueKind == JsonValueKind.Number)
            return property.TryGetDouble(out value);

        if (property.ValueKind == JsonValueKind.String &&
            double.TryParse(
                property.GetString(),
                NumberStyles.Float | NumberStyles.AllowThousands,
                CultureInfo.InvariantCulture,
                out value))
        {
            return true;
        }

        return false;
    }

    private static void TryTerminateProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Intentionally ignored: process teardown is best-effort.
        }
    }
}
