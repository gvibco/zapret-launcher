using System.Diagnostics;
using System.Net.NetworkInformation;

namespace ZapretLauncher.Core.Networking;

public sealed class NetworkKeyProvider
{
    public string GetKey()
    {
        var nic = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up)
            .Where(n => n.NetworkInterfaceType is NetworkInterfaceType.Ethernet
                or NetworkInterfaceType.Wireless80211)
            .OrderByDescending(n => n.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
            .FirstOrDefault();

        if (nic is null) return "unknown";

        var gateway = nic.GetIPProperties().GatewayAddresses
            .FirstOrDefault()?.Address.ToString() ?? "no-gw";
        return BuildKey(nic.Id, gateway, TryGetSsid(nic.Name));
    }

    public static string BuildKey(string id, string gateway, string? ssid) =>
        $"{id}|{gateway}|{ssid ?? ""}";

    public static string? ParseSsid(string output)
    {
        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("SSID", StringComparison.OrdinalIgnoreCase)) continue;
            var parts = trimmed.Split(':', 2);
            if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]))
                return parts[1].Trim();
        }
        return null;
    }

    private static string? TryGetSsid(string adapterName)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("netsh", "wlan show interfaces")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            if (process is null) return null;
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(2000);
            return ParseSsid(output);
        }
        catch
        {
            return null;
        }
    }
}
