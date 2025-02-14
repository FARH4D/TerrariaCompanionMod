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

namespace TerrariaCompanionApp
{
public class TerrariaCompanionApp : Mod
{
    private TcpListener _server;
    private Texture2D texture;
    private bool _serverRunning = false;
    public Dictionary<int, List<Recipe>> recipe_dict;
    private String current_page;

    public override void Load()
    {
        base.Load();

    }
    
    public override void Unload()
    {
        base.Unload();
    }

    public override void PostSetupContent()
    {
        base.PostSetupContent();
        //getTexture();
        
    }

    private void StartServer()
    {
        _serverRunning = true;



        _server = new TcpListener(IPAddress.Any, 12345); // Use a port like 12345
        _server.Start();
        _server.BeginAcceptTcpClient(HandleClient, null);
    }

    private void getTexture()
    {

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

        recipe_dict = new Dictionary<int, List<Recipe>>();
        BestiaryDatabase bestiaryDB = Main.BestiaryDB;

        
        

        // if (bossList == null)
        // {
        //     Main.NewText("Boss Checklist data not available yet.");
        // }


        Mod bossChecklistMod = ModLoader.GetMod("BossChecklist");
        var bossList = bossChecklistMod.Call("GetBossInfoDictionary", this) as Dictionary<string, Dictionary<string, object>>;

        List<string> boss_list_names = new List<string>();
        // string texture_path;
        // string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        



        Main.QueueMainThreadAction(() => {
        foreach (KeyValuePair<string, Dictionary<string, object>> kvp in bossList)
        {

            Dictionary<string, object> entryInfo = kvp.Value;


            bool isBoss = false;
            if (entryInfo.TryGetValue("isBoss", out object isBossObj))
            {
                isBoss = Convert.ToBoolean(isBossObj);
            }

            string bossName = "";
            List<int> npc_ids = [];

            entryInfo.TryGetValue("key", out object keyObj);
            bossName = keyObj as string;    
            boss_list_names.Add(bossName);
            

            Main.NewText(bossName);

            if (entryInfo.TryGetValue("spawnInfo", out object spawninfoObj) && spawninfoObj is Func<LocalizedText> spawnInfoFunc) {
                LocalizedText spawnInfoText = spawnInfoFunc();
                string spawnInfoString = spawnInfoText.Value;
                Main.NewText(spawnInfoString);
            }

            // if (entryInfo.TryGetValue("dropRateInfo", out object dropRatesObj) && dropRatesObj is List<DropRateInfo> dropRates){
                
            //     foreach (DropRateInfo drop_item in dropRates) {
            //         Main.NewText(drop_item.itemId);
            //         Main.NewText(drop_item.dropRate);
            //     }
            // }

            // if (entryInfo.TryGetValue("npcIDs", out object npcIDsObj) && npcIDsObj is List<int> npcIDList)
            // {
            //     if (npcIDList != null || npcIDList.Count != 0)
            //     {
                    
            //         foreach (int npcID in npcIDList)
            //         {
            //             if (npcID > NPCID.Count && npcID != 0) {
            //                 // texture_path = ModContent.GetModNPC(npcID).Texture;
            //                 Main.NewText(ModContent.GetModNPC(npcID).FullName);
            //                 // Asset<Texture2D> textureAsset = ModContent.Request<Texture2D>(texture_path);
            //                 // Texture2D texture = textureAsset.Value;
            //                 // string filePath = Path.Combine(downloadsPath, $"npc{npcID}.png");
            //                 // int frameCount = Main.npcFrameCount[npcID]; // Get number of frames
            //                 // if (frameCount <= 0) frameCount = 1; // Ensure we don’t divide by 0

            //                 // int frameWidth = texture.Width;
            //                 // int frameHeight = texture.Height / frameCount;

            //                 // Texture2D firstFrameTexture = new Texture2D(Main.graphics.GraphicsDevice, frameWidth, frameHeight);
            //                 // Microsoft.Xna.Framework.Color[] fullPixels = new Microsoft.Xna.Framework.Color[texture.Width * texture.Height];
            //                 // texture.GetData(fullPixels);

            //                 // Microsoft.Xna.Framework.Color[] framePixels = new Microsoft.Xna.Framework.Color[frameWidth * frameHeight];
            //                 // for (int y = 0; y < frameHeight; y++)
            //                 // {
            //                 //     for (int x = 0; x < frameWidth; x++)
            //                 //     {
            //                 //         framePixels[y * frameWidth + x] = fullPixels[y * frameWidth + x];
            //                 //     }
            //                 // }

            //                 // firstFrameTexture.SetData(framePixels);

            //                 // using (MemoryStream ms = new MemoryStream())
            //                 // {
            //                 //     firstFrameTexture.SaveAsPng(ms, firstFrameTexture.Width, firstFrameTexture.Height);
            //                 //     File.WriteAllBytes(filePath, ms.ToArray());

            //                 //     Main.NewText($"First frame saved as {filePath}");
            //                 // }
            //                 // Main.NewText("yup");

                            
            //                 // SaveTextureToFile(textureAsset, filePath);
            //                 // Main.NewText(texture_path);
            //                 // Main.NewText("saved");
            //             }
            //         }
                    
            //     }

            // }

        }
        });

        
        
        

        while (_serverRunning && client.Connected)
        {
            try {
                // Send data to the client
                string data = GetData() + "\n";
                byte[] buffer = Encoding.UTF8.GetBytes(data);
                stream.Write(buffer, 0, buffer.Length);
                stream.Flush();




                Main.QueueMainThreadAction(() => {
                    

                    // Main.NewText(player.buffType[0]);

                        

                // Main.NewText(Main.item[5673].Name);
                // if (Main.item[5673].)

                // foreach (var mod in ModLoader.Mods) {

                //     for (int i = 0; i < Recipe.numRecipes; i++){

                //         Main.recipe[i].createItem.Name

                //     }

                // }


                // string filePath = @"E:\Downloads\playerthing.png";
                // Asset<Texture2D> playerhair = ModContent.Request<Texture2D>("Terraria/Images/Player_Hair_" + ((int)player.hair + 1));

                // Texture2D texture = playerhair.Value;

                // Texture2D colouredTexture = new Texture2D(texture.GraphicsDevice, texture.Width, texture.Height);
                // Microsoft.Xna.Framework.Color[] originalPixels = new Microsoft.Xna.Framework.Color[texture.Width * texture.Height];
                // Microsoft.Xna.Framework.Color[] colouredPixels = new Microsoft.Xna.Framework.Color[texture.Width * texture.Height];

                // texture.GetData(originalPixels);

                // for (int i = 0; i < originalPixels.Length; i++)
                // {
                //     Microsoft.Xna.Framework.Color original = originalPixels[i];

                //     if (original.A > 0)
                //     {
                //         byte r = (byte)(original.R * player.hairColor.R / 255);
                //         byte g = (byte)(original.G * player.hairColor.G / 255);
                //         byte b = (byte)(original.B * player.hairColor.B / 255);
                //         byte a = original.A;

                //         colouredPixels[i] = Microsoft.Xna.Framework.Color.FromNonPremultiplied(r, g, b, a);

                //     }
                //     else
                //     {
                //         colouredPixels[i] = original;
                //     }
                // }

                // colouredTexture.SetData(colouredPixels);

                // using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                // {
                //     colouredTexture.SaveAsPng(fileStream, colouredTexture.Width, colouredTexture.Height);
                // }

                // ColorHair(playerhair, newHairColour, filePath);


                // Asset<Texture2D> ironPickaxe2 = ModContent.Request<Texture2D>("Terraria/Images/Player_0_1.xnb");
                // Asset<Texture2D> ironPickaxe3 = ModContent.Request<Texture2D>("Terraria/Images/Player_1_0.xnb");
                // SaveTextureToFile(playerhair, filePath);
                // SaveTextureToFile(ironPickaxe2, filePath2);
                // SaveTextureToFile(ironPickaxe3, filePath3);
                    });


        //         for (int npcID = NPCID.NegativeIDCount; npcID < NPCLoader.NPCCount; npcID++)
        // {
        //     ModNPC modNPC = NPCLoader.GetNPC(npcID);
        //     string npcName = modNPC != null ? modNPC.FullName : Lang.GetNPCNameValue(npcID);

        //     Main.NewText($"NPC ID: {npcID}, Name: {npcName}");
        //     // Texture2D npcTexture = ModContent.Request<Texture2D>("CalamityMod/NPCTextureName").Value;
        // }

        // if (ModLoader.TryGetMod("CalamityMod", out Mod calamityMod) && calamityMod.TryFind<ModItem>("ElementalGauntlet", out ModItem ElementalGauntlet)) {
        //     string filePath = @"E:\Downloads\playerthing.png";
        //     Asset<Texture2D> itemTexture = ModContent.Request<Texture2D>(ElementalGauntlet.Texture);
        //     SaveTextureToFile(itemTexture, filePath);
        // };


                // Main.QueueMainThreadAction(() => {

                //     Main.NewText("testing");
                //     if (ModLoader.TryGetMod("CalamityMod", out Mod calamityMod) && calamityMod.TryFind<ModItem>("ElementalGauntlet", out ModItem ElementalGauntlet)) {
                //         string filePath             // Extract health & mana

                // });
                System.Threading.Thread.Sleep(1000); // Send updates every 1 second

            }
            catch (Exception e){

            }
        }

        client.Close();
    }

    private void SaveTextureToFile(Asset<Texture2D> textureAsset, string fileName)
    {
        try
        {
            // Extract the actual Texture2D from the Asset<Texture2D>
            Texture2D texture = textureAsset.Value;

            // Get a memory stream to save the PNG to
            using (MemoryStream ms = new MemoryStream())
            {
                // Save the texture as PNG to the memory stream
                texture.SaveAsPng(ms, texture.Width, texture.Height);

                // Write the PNG data to a file
                File.WriteAllBytes(fileName, ms.ToArray());

                Main.NewText($"Texture saved as {fileName}");
            }
        }
        catch (Exception e)
        {
            Main.NewText($"Error saving texture: {e.Message}");
        }
    }

    private string GetData()
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
}