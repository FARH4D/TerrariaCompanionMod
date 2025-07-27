using Terraria.ModLoader.Config;
using System.ComponentModel;
using Terraria.ModLoader;

public class ServerConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("ConnectionInfo")]

    [Label("Local IP Address")]
    [Tooltip("Your local IP address (used for connecting to your mobile app)")]
    [DefaultValue("Unavailable")]
    public string LocalIPAddressDisplay = "Unavailable";

    [Label("Server Port")]
    [Tooltip("The port used by the mod's server")]
    [Range(1024, 65535)]
    [DefaultValue(12345)]
    public int ServerPort = 12345;

    public static ServerConfig Instance => ModContent.GetInstance<ServerConfig>();

    public override void OnChanged()
    {
        LocalIPAddressDisplay = ServerSystem.GetLocalIPAddress(); // Resets ip if the user edits
    }

}