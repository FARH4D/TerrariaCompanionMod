using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using System.IO;
using Terraria.ModLoader;
using System.Drawing;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ObjectInteractions;
using Terraria.GameContent.ItemDropRules;
using Terraria.Localization;
using System.Threading;
using System.Threading.Tasks;

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

    private TerrariaCompanionMod.LoadItems _itemLoader;
    private TerrariaCompanionMod.LoadNpcs _npcLoader;
    
    private ModServer() { }

    public override void OnWorldLoad()
    {
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
        _itemLoader = new TerrariaCompanionMod.LoadItems();
        _npcLoader = new TerrariaCompanionMod.LoadNpcs();
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
                        Main.NewText(receivedMessage);

                        if (receivedMessage.Contains(":")){
                            string[] parts = receivedMessage.Split(":", 3);
                            page_name = parts[0];
                            item_num = int.Parse(parts[1]);
                            category = parts[2];
                            
                        }

                        _currentPage = page_name;
                        _currentNum = item_num;
                        _category = category;
                        
                    }

                    if (_client != null && _client.Connected)
                    {

                        if (_currentPage == "HOME")
                        {
                            // Send data to the client
                            string data = GetHomeData();
                            byte[] buffer = Encoding.UTF8.GetBytes(data + "\n");
                            _stream.Write(buffer, 0, buffer.Length);
                            _stream.Flush();
                        }
                        else if (_currentPage == "RECIPES")
                        {
                            _stream.Flush();
                            if (_currentNum != _lastNum) {
                                _lastNum = _currentNum;
                                string data = await GetDataForPage();
                                byte[] buffer = Encoding.UTF8.GetBytes(data + "\n");
                                _stream.Write(buffer, 0, buffer.Length);
                                _stream.Flush();
                            }
                        }
                        else if (_currentPage == "BEASTIARY")
                        {
                            if (_currentNum != _lastNum) {
                                _lastNum = _currentNum;
                                string data = await GetDataForPage();
                                byte[] buffer = Encoding.UTF8.GetBytes(data + "\n");
                                _stream.Write(buffer, 0, buffer.Length);
                                _stream.Flush();
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

    public void StopServer()
    {
        _running = false;
        _server?.Stop();
        _client?.Close();
    }

    public async Task<string> GetDataForPage()
    {   
        if (_currentPage == "HOME") return GetHomeData();
        if (_currentPage == "RECIPES") {
            return await _itemLoader.LoadItemList(_currentNum, _category.ToString()); 
            }
        if (_currentPage == "BEASTIARY") {
            return await _npcLoader.LoadNpcList(_currentNum, _category.ToString()); 
            }
        return "Unknown Page";
    }

    private string GetHomeData()
    {
        List<string> playerNames = new List<string>();
    
        foreach (var playername in Main.ActivePlayers) {
            playerNames.Add(playername.name);
        }

        Player player = Main.LocalPlayer; // Get the local player

        var data = new {
            health = new {current = player.statLife, max = player.statLifeMax},
            mana = new {current = player.statMana, max = player.statManaMax},
            player_list = playerNames
        };

        return JsonConvert.SerializeObject(data);
    }
}
