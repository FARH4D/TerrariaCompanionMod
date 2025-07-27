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
        // This is to get rid of IP ranges like Docker, NordVPN, VirtualBox that would give the user the incorrect IP
        return ip.StartsWith("10.5.") || ip.StartsWith("192.168.56."); // I'll probably need to add more eventually
    }
}