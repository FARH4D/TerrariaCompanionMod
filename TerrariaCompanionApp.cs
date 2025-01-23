using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Terraria;
using Terraria.ModLoader;

namespace TerrariaCompanionApp
{
    public class TerrariaCompanionApp : Mod
    {
        private TcpListener _server;
        private bool _serverRunning = false;

        public override void Load()
        {
            base.Load();
            StartServer();
        }

        public override void Unload()
        {
            base.Unload();
            StopServer();
        }

        private void StartServer()
        {
            _serverRunning = true;
            _server = new TcpListener(IPAddress.Any, 12345); // Use a port like 12345
            _server.Start();

            _server.BeginAcceptTcpClient(HandleClient, null);
        }

        private void StopServer()
        {
            _serverRunning = false;
            _server?.Stop();
        }

        private void HandleClient(IAsyncResult result)
        {
            if (!_serverRunning) return;

            TcpClient client = _server.EndAcceptTcpClient(result);
            NetworkStream stream = client.GetStream();

            // Send data to the client
            string data = GetPlayerData();
            byte[] buffer = Encoding.UTF8.GetBytes(data);
            stream.Write(buffer, 0, buffer.Length);

            // Close the connection
            client.Close();

            // Continue listening for other clients
            _server.BeginAcceptTcpClient(HandleClient, null);
        }

        private string GetPlayerData()
        {
            Player player = Main.LocalPlayer; // Get the local player
            return $"HP:{player.statLife},MP:{player.statMana}";
        }
    }
}
