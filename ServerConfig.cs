using Terraria.ModLoader.Config;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using Terraria.ModLoader;

public class ServerConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ServerSide;

    [Header("ConnectionInfo")]

    [Label("Local IP Address")]
    [Tooltip("Your local IP address on the network (used for connecting your mobile app)")]
    [JsonIgnore]
    public string LocalIPAddress => GetLocalIPAddress();

    [Label("Server Port")]
    [Tooltip("The port used by the mod's server communication system")]
    [Range(1024, 65535)]
    [DefaultValue(12345)]
    public int ServerPort = 12345;

    public static ServerConfig Instance => ModContent.GetInstance<ServerConfig>();

    private static string GetLocalIPAddress()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            return (socket.LocalEndPoint as IPEndPoint)?.Address?.ToString() ?? "Unavailable";
        }
        catch
        {
            return "Unavailable";
        }
    }
}