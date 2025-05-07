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
using System.Reflection;
using TerrariaCompanionMod;

public class ModServer : ModSystem
{
    private static ModServer _instance;
    private TcpListener _server;
    private TcpClient _client;
    private NetworkStream _stream;
    private Thread _listenerThread;

    private bool _running = false;
    private string _currentPage;
    private int _currentNum;
    private int _lastNum = 0;
    private string _category = "all";
    private string _lastCategory = "";
    private bool _firstChecklist = false;

    private LoadItems _itemLoader;
    private LoadNpcs _npcLoader;
    private LoadChecklist _checklistLoader;
    private NpcPage _npcPage;
    private ItemPage _itemPage;
    private BossPage _bossPage;
    
    private ModServer() { }

    public override void OnWorldLoad()
    {
        _lastNum = 0;
        _currentPage = "HOME";
        StartServer();
    }

    public override void OnWorldUnload()
    {
        StopServer();
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

    public void StartServer(int port = 12345)
    {
        _itemLoader = new LoadItems();
        _npcLoader = new LoadNpcs();
        _checklistLoader = new LoadChecklist();
        _npcPage = new NpcPage();
        _itemPage = new ItemPage();
        _bossPage = new BossPage();

        if (_running) return; // Prevent multiple starts
        _running = true;

        _server = new TcpListener(IPAddress.Any, port);
        _server.Start();

        _listenerThread = new Thread(async () =>
        {
            while (_running)
            {
                try {

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

                        if (receivedMessage.Contains(":")){
                            string[] parts = receivedMessage.Split(":", 3);
                            page_name = parts[0];
                            item_num = int.Parse(parts[1]);
                            category = parts[2];
                            
                        }
                        _currentPage = page_name;
                        _currentNum = item_num;
                        _category = category;

                        Main.NewText(receivedMessage);
                        if (_category != _lastCategory){
                            _lastNum = -1;
                            _lastCategory = category;
                        }
                    }

                    if (_client != null && _client.Connected)
                    {
                        if (_currentPage == "HOME")
                        {
                            _firstChecklist = false;
                            _lastNum = 0;
                            SendData(GetHomeData());
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
                }
                catch (Exception e) {
                    StopServer();
                    break;
                }

                Thread.Sleep(1000);
            }
        });
        _listenerThread.Start();
    }

    private void SendData(string data)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(data + "\n");
        _stream.Write(buffer, 0, buffer.Length);
        _stream.Flush();
    }

    public void StopServer()
    {
        _running = false;
        _server?.Stop();
        _client?.Close();
    }

    public async Task<string> GetDataForPage() => _currentPage switch
    {
        "RECIPES" => await _itemLoader.LoadItemList(_currentNum, _category.ToString()),
        "BEASTIARY" => await _npcLoader.LoadNpcList(_currentNum, _category.ToString()),
        "BEASTIARYINFO" => await _npcPage.LoadData(_currentNum),
        "ITEMINFO" => await _itemPage.LoadData(_currentNum),
        "CHECKLIST" => await _checklistLoader.LoadBosses(),
        "BOSSINFO" => await _bossPage.LoadData(_currentNum),
        _ => "Unknown Page"
    };

    private string GetHomeData()
    {
        List<string> playerNames = new List<string>();
    
        foreach (var playername in Main.ActivePlayers) {
            playerNames.Add(playername.name);
        }

        Player player = Main.LocalPlayer;

        var data = new {
            health = new {current = player.statLife, max = player.statLifeMax},
            mana = new {current = player.statMana, max = player.statManaMax},
            player_list = playerNames
        };

        return JsonConvert.SerializeObject(data);
    }
}
