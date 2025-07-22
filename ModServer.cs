using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Terraria;
using Terraria.ModLoader;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using TerrariaCompanionMod;
using System.IO;

public class ModServer : ModSystem
{
    private static ModServer _instance;
    private TcpListener _server;
    private TcpClient _client;
    private NetworkStream _stream;
    private Thread _listenerThread;
    private bool _isRestarting = false;
    private bool _running;
    public bool IsRunning => _running;
    private int _lastPort = -1;
    private string _currentPage;
    private int _currentNum;
    private string _search;
    private int _lastNum = 0;
    private int[] _trackedItems = new int[0];
    private string _category = "null";
    private string _lastCategory = "";
    private string _lastSearch = "";
    private bool _firstChecklist = false;
    private bool _loadPlayerCosmetics = false;

    private LoadItems _itemLoader;
    private LoadNpcs _npcLoader;
    private LoadChecklist _checklistLoader;
    private NpcPage _npcPage;
    private ItemPage _itemPage;
    private BossPage _bossPage;
    private PotionLoadouts _potionLoadouts;
    private UsePotions _usePotions;
    List<PotionEntryData> potions;

    private ModServer() { }

    public override void OnWorldLoad()
    {
        _lastNum = 0;
        StartServer();
    }

    public override void OnWorldUnload()
    {
        StopServer();
    }

    public override void PostUpdateEverything()
    {
        if (!_running)
            return;

        int currentPort = ServerConfig.Instance.ServerPort;

        if (currentPort != _lastPort)
        {
            RestartServer();
            Main.NewText("Server restarted on new port: " + currentPort);
            _lastPort = currentPort;
        }
    }

    public static ModServer Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new ModServer();
            }
            return _instance;
        }
    }

    public void StartServer(int? port = null)
    {
        int selectedPort = port ?? ServerConfig.Instance.ServerPort;
        _lastPort = selectedPort;
        _running = false;
        
        _itemLoader = new LoadItems();
        _npcLoader = new LoadNpcs();
        _checklistLoader = new LoadChecklist();
        _npcPage = new NpcPage();
        _itemPage = new ItemPage();
        _bossPage = new BossPage();
        _potionLoadouts = new PotionLoadouts();
        _usePotions = new UsePotions();

        if (_running) return; // Prevent multiple starts
        _running = true;

        _server = new TcpListener(IPAddress.Any, selectedPort);
        _server.Start();

        _listenerThread = new Thread(async () =>
        {
            while (_running)
            {
                try
                {
                    if (_server.Pending()) // Check for new clients
                    {
                        _client = _server.AcceptTcpClient();
                        _stream = _client.GetStream();
                        Main.NewText("Connected to Terraria Companion App!");
                    }

                    if (_client != null && _client.Connected && _stream.DataAvailable)
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                        string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                        string page_name = receivedMessage;
                        int item_num = 30;
                        string category = "all";
                        string search = "";

                        if (receivedMessage.StartsWith("USELOADOUT_BASE64:"))
                        {
                            Mod.Logger.Info(receivedMessage);
                            item_num = 0;
                            page_name = "NULL";
                            category = "SKIPCONDITION";

                            string base64 = receivedMessage.Substring("USELOADOUT_BASE64:".Length);
                            string json = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                            try
                            {
                                potions = JsonConvert.DeserializeObject<List<PotionEntryData>>(json);
                            }
                            catch (Exception ex)
                            {
                                Mod.Logger.Error("Failed to deserialize USELOADOUT JSON: " + ex.Message);
                            }
                            _usePotions.consumePotions(Main.LocalPlayer, potions);
                        }

                        else if (receivedMessage.Contains(":"))
                        {
                            string[] parts = receivedMessage.Split(":", 3);
                            page_name = parts[0];
                            item_num = int.Parse(parts[1]);

                            if (parts[2].Contains(","))
                            {
                                string[] subParts = parts[2].Split(',', 2);
                                category = subParts[0];
                                search = subParts.Length > 1 ? subParts[1] : "";
                            }
                            else
                            {
                                category = parts[2];
                                search = "";
                            }
                        }

                        _currentNum = item_num;
                        _category = category;
                        _currentPage = page_name;
                        _search = search;

                        if (_category != _lastCategory)
                        {
                            _lastNum = -1;
                            _lastCategory = _category;
                        }

                        if (_search != _lastSearch)
                        {
                            _lastNum = -1;
                            _lastSearch = _search;
                        }
                    }

                    if (_client != null && _client.Connected)
                    {
                        try
                        {
                            if (_currentPage == "HOME")
                            {
                                _firstChecklist = false;
                                _lastNum = 0;

                                SendData(GetHomeData());
                            }
                            else if (_currentPage == "NULL")
                            {
                                _lastNum = 0;
                            }
                            else
                            {
                                if (_currentNum != _lastNum || !_firstChecklist)
                                {
                                    _lastNum = _currentNum;
                                    string data = await GetDataForPage();
                                    SendData(data);
                                    _firstChecklist = true;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!ex.Message.Contains("forcibly closed by the remote host"))
                            {
                                Mod.Logger.Warn("Unhandled error: " + ex);
                            }
                            DisconnectClient();
                            Main.NewText("Disconnected");
                            RestartServer();
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("forcibly closed by the remote host"))
                    {
                        Mod.Logger.Warn("Unhandled error in listenerThread: " + ex);
                    }

                    DisconnectClient();
                    Main.NewText("Client disconnected. Restarting server.");

                    _ = Task.Run(() => RestartServer()); // Using this method so I can suppress the await warning
                }

                Thread.Sleep(1000);
            }
        });
        _listenerThread.Start();
    }

    private void SendData(string data)
    {
        if (_stream == null || !_client.Connected) return;

        try
        {
            byte[] buffer = Encoding.UTF8.GetBytes(data + "\n");
            _stream.Write(buffer, 0, buffer.Length);
            _stream.Flush();
        }
        catch (Exception ex) when (ex is IOException || ex is SocketException || ex is ObjectDisposedException)
        {
            // Mod.Logger.Warn($"Exception in the SendData method: {ex.Message}");
            DisconnectClient();

            Task.Run(() => RestartServer());
        }
    }

    private void DisconnectClient()
    {
        try
        {
            _stream?.Close();
            _client?.Close();
            _stream = null;
            _client = null;
        }
        catch (Exception e)
        {
            Mod.Logger.Warn("Error while disconnecting client: " + e.Message);
        }
    }

    public void StopServer()
    {
        try
        {
            _running = false;
            _stream?.Close();
            _client?.Close();
            _server?.Stop();

            _listenerThread?.Join();
        }
        catch (Exception ex)
        {
            Mod.Logger.Error("Error while stopping server: " + ex.Message);
        }
    }
    public void RestartServer()
    {
        if (_isRestarting) return;

        _isRestarting = true;

        StopServer();
        Thread.Sleep(250);
        StartServer();

        _isRestarting = false;
    }

    public async Task<string> GetDataForPage() => _currentPage switch
    {
        "RECIPES" => await _itemLoader.LoadItemList(_currentNum, _category.ToString(), _search),
        "BEASTIARY" => await _npcLoader.LoadNpcList(_currentNum, _search),
        "BEASTIARYINFO" => await _npcPage.LoadData(_currentNum),
        "ITEMINFO" => await _itemPage.LoadData(_currentNum),
        "CHECKLIST" => await _checklistLoader.LoadBosses(),
        "BOSSINFO" => await _bossPage.LoadData(_currentNum),
        "CREATEPOTION" => await _potionLoadouts.GetConsumablesData(Main.LocalPlayer),
        _ => "Unknown Page"
    };

    private string GetHomeData()
    {
        List<string> playerNames = new List<string>();

        foreach (var playername in Main.ActivePlayers)
        {
            playerNames.Add(playername.name);
        }

        Player player = Main.LocalPlayer;

        Dictionary<string, string> visualData = new();

        if (!_loadPlayerCosmetics)
        {
            visualData = PlayerRender.GetPlayerVisualBase64(player);
        }

        string biome = GetPlayerBiome(player);

        if (_currentNum != 0)
        {
            _trackedItems = _itemLoader.ingredientNum(player, _currentNum);
        }

        var data = new
        {
            health = new { current = player.statLife, max = player.statLifeMax },
            mana = new { current = player.statMana, max = player.statManaMax },
            player_list = playerNames,
            cosmetics = visualData,
            biome = biome,
            _trackedItems
        };

        return JsonConvert.SerializeObject(data);
    }

    public static string GetPlayerBiome(Player player)
    {
        string elevation = "";
        if ((int)(player.position.Y / 16) >= Main.maxTilesY - 200) elevation = "underworld";
        // else if (player.ZoneSkyHeight) elevation = "sky";
        else if (player.ZoneOverworldHeight) elevation = "surface";
        else if (player.ZoneDirtLayerHeight) elevation = "underground";
        else if (player.ZoneRockLayerHeight) elevation = "cavern";

        if (player.ZoneDungeon) return "dungeon";
        if (player.ZoneJungle) return elevation + "_jungle";
        if (player.ZoneCorrupt) return elevation + "_corruption";
        if (player.ZoneCrimson) return elevation + "_crimson";
        if (player.ZoneHallow) return elevation + "_hallow";
        if (player.ZoneSnow) return elevation + "_snow";
        if (player.ZoneDesert) return elevation + "_desert";
        if (player.ZoneGlowshroom) return elevation + "_glowing_mushroom";
        if (player.ZoneBeach) return "ocean";
        if (player.ZoneGranite) return "granite";
        if (player.ZoneMarble) return "marble";
        if (player.ZoneHive) return "bee_hive";
        if (player.ZoneTowerNebula) return "nebula";
        if (player.ZoneTowerSolar) return "solar";
        if (player.ZoneTowerVortex) return "vortex";
        if (player.ZoneTowerStardust) return "stardust";
        if (player.ZoneForest) return "forest";

        else return elevation;
    }
}
