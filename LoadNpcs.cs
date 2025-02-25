using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace TerrariaCompanionApp
{
    public class LoadNpcs : ModSystem
    {
        private List<Dictionary<string, object>> _currentList;
        private bool hasLoaded = false;
        private HashSet<int> npcsToProcess;

        public override void Load()
        {
            NpcStorage.Init(Mod);
            npcsToProcess = new HashSet<int>();
        }

        // public override void PostSetupContent()
        // {
        //     LoadAllNpcTextures();
        // }

        public override void PostUpdatePlayers()
        {
            if (!Main.gameMenu && !hasLoaded) // Ensure it's only executed once
            {
                hasLoaded = true;
                Main.QueueMainThreadAction(() =>
                {
                    LoadAllNpcTextures();
                });
            }
        }

        public void LoadAllNpcTextures()
        {
            var mainList = new Dictionary<int, Dictionary<string, object>>();
            var storage = NpcStorage.Instance;

            try
            {
                for (int i = 0; i < NPCLoader.NPCCount; i++)
                {
                    NPC npc = new NPC();
                    npc.SetDefaults(i);
                    
                    if (string.IsNullOrEmpty(npc.FullName)) // Skip invalid NPCs
                        continue;

                    npcsToProcess.Add(i); // Track NPCs being processed
                    
                    Main.QueueMainThreadAction(() =>
                    {
                        try
                        {
                            Texture2D currentTexture;

                            Mod.Logger.Info($"found {npc.type}");
                            
                            Main.instance.LoadNPC(npc.type);

                            if (TextureAssets.Npc[npc.type] == null)
                            {
                                Mod.Logger.Warn($"Texture not found for NPC: {npc.FullName}");
                                return;
                            }

                            currentTexture = TextureAssets.Npc[npc.type].Value;
                            

                            string base64Image = ExtractFirstFrame(currentTexture, npc);
                            
    

                            // string base64Image = ConvertTextureToBase64(currentTexture);

                            var npcDict = new Dictionary<string, object>
                            {
                                {"name", npc.FullName},
                                {"id", npc.type},
                                {"image", base64Image}
                            };
                            Mod.Logger.Info(npc.FullName);

                            mainList[npc.type] = npcDict;
                        }
                        catch (Exception innerEx)
                        {
                            Mod.Logger.Warn($"Error processing NPC texture: {innerEx}");
                        }

                        npcsToProcess.Remove(i);
                    });
                }
            }
            catch (Exception ex)
            {
                Mod.Logger.Warn($"Unexpected error in LoadAllNpcTextures: {ex}");
            }

            storage.SetMainList(mainList); // Store the NPC list in storage
        }

        public override void OnWorldUnload()
        {
            var storage = NpcStorage.Instance;

            try
            {
                storage.ClearMainList();
                hasLoaded = false;
                Mod.Logger.Info("World unloaded and NPC lists cleaned up.");
            }
            catch (Exception ex)
            {
                Mod.Logger.Warn($"Error cleaning up on world unload: {ex}");
            }
        }

        public async Task<string> LoadNpcList(int max, string category)
        {
            return await Task.Run(() =>
            {
                var storage = NpcStorage.Instance;
                var _mainList = storage.GetMainList();

                if (_mainList == null || _mainList.Count == 0)
                {
                    return "Error: No NPCs found!";
                }

                Main.NewText(_mainList.Count);

                List<Dictionary<string, object>> listToUse;

                listToUse = _mainList.Values.ToList();


                _currentList = listToUse.Skip(Math.Max(0, listToUse.Count - max)).Take(30).ToList();
                return JsonConvert.SerializeObject(_currentList);
            });
        }

        private string ConvertTextureToBase64(Texture2D texture)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                texture.SaveAsPng(ms, texture.Width, texture.Height);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        private string ExtractFirstFrame(Texture2D texture, NPC npc)
        {
            
            int frameCount = Main.npcFrameCount[npc.type]; // Get number of frames
            if (frameCount <= 0) frameCount = 1; // Ensure we don’t divide by 0

            int frameWidth = texture.Width;
            int frameHeight = texture.Height / frameCount;

            Texture2D firstFrameTexture = new Texture2D(Main.graphics.GraphicsDevice, frameWidth, frameHeight);
            Microsoft.Xna.Framework.Color[] fullPixels = new Microsoft.Xna.Framework.Color[texture.Width * texture.Height];
            texture.GetData(fullPixels);



            Microsoft.Xna.Framework.Color[] framePixels = new Microsoft.Xna.Framework.Color[frameWidth * frameHeight];
            for (int y = 0; y < frameHeight; y++)
            {
                for (int x = 0; x < frameWidth; x++)
                {
                    framePixels[y * frameWidth + x] = fullPixels[y * frameWidth + x];
                }
            }


            firstFrameTexture.SetData(framePixels);
            
            return ConvertTextureToBase64(firstFrameTexture);

        }

    }
}
