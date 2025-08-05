using Terraria.ModLoader;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Net;

public class ServerSystem : ModSystem
{
    public override void OnWorldLoad()
    {
        ServerConfig.Instance.LocalIPAddressDisplay = GetLocalIPAddress();
    }

    public static string GetLocalIPAddress()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ipconfig",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var matches = Regex.Matches(output, @"IPv4 Address[.\s]*:\s*(\d+\.\d+\.\d+\.\d+)");

            foreach (Match match in matches)
            {
                string ip = match.Groups[1].Value;
                if (IsPrivateIPv4(ip) && !IsBadAdapter(ip))
                    return ip;
            }

            return "Unavailable";
        }
        catch
        {
            return "Unavailable";
        }
    }

    private static bool IsPrivateIPv4(string ip)
    {
        if (IPAddress.TryParse(ip, out IPAddress? address))
        {
            byte[] bytes = address.GetAddressBytes();
            return
                (bytes[0] == 10) ||
                (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                (bytes[0] == 192 && bytes[1] == 168);
        }
        return false;
    }

    private static bool IsBadAdapter(string ip)
    {
        return
            ip.StartsWith("10.5.") ||                 // Docker default bridge
            ip.StartsWith("10.8.") ||                 // OpenVPN / Tunnelblick default
            ip.StartsWith("10.9.") ||                 // ZeroTier / VPN
            ip.StartsWith("10.10.") ||                // Tailscale (old)
            ip.StartsWith("100.64.") ||               // CGNAT / Tailscale
            ip.StartsWith("172.16.") ||               // Often used by Docker / VPNs
            ip.StartsWith("172.17.") ||               // Docker default
            ip.StartsWith("172.18.") ||               // Docker bridge networks
            ip.StartsWith("172.19.") ||               // Docker networks
            ip.StartsWith("172.20.") ||               // Docker / Hyper-V
            ip.StartsWith("192.168.56.") ||           // VirtualBox Host-Only
            ip.StartsWith("192.168.137.") ||          // VMware VMnet1 / ICS (Internet Connection Sharing)
            ip.StartsWith("192.168.100.") ||          // QEMU / Tailscale / Hyper-V Switch
            ip.StartsWith("169.254.") ||              // Link-local (invalid for LAN)
            ip.StartsWith("0.") ||                    // Invalid / misconfigured
            ip.Equals("127.0.0.1");                   // Loopback
    }
}