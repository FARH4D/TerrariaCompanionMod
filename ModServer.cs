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

    private LoadItems _itemLoader;
    
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
        _itemLoader = new LoadItems();
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

                        if (receivedMessage.Contains(":")){
                            Main.NewText(receivedMessage);
                            string[] parts = receivedMessage.Split(":", 2);
                            page_name = parts[0];
                            item_num = int.Parse(parts[1]);
                            Main.NewText(item_num);
                        }

                        _currentPage = page_name;
                        _currentNum = item_num;
                        
                    }


                    if (_client != null && _client.Connected)
                    {

                        if (_currentPage == "HOME")
                        {
                            Main.NewText("home");
                            // Send data to the client
                            string data = await GetDataForPage(_currentPage, _currentNum);
                            byte[] buffer = Encoding.UTF8.GetBytes(data + "\n");
                            _stream.Write(buffer, 0, buffer.Length);
                            _stream.Flush();
                        }
                        else if (_currentPage == "RECIPES")
                        {
                            if (_currentNum != _lastNum) {
                                Main.NewText("done?");
                                _lastNum = _currentNum;
                                string data = await GetDataForPage(_currentPage, _currentNum);
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

    public async Task<string> GetDataForPage(string page, int current_num)
    {
        if (page == "HOME") return GetHomeData();
        if (page == "RECIPES") return await _itemLoader.LoadItemList(current_num); // Await the async method
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
