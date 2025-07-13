using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Linq;

namespace TerrariaCompanionMod
{
    public class LoadNpcs : ModSystem
    {
        private List<Dictionary<string, object>> _currentList;
        private bool hasLoaded = false;
        private string warning;
        private HashSet<int> npcsToProcess;

        public override void Load()
        {
            NpcStorage.Init(Mod);
            npcsToProcess = new HashSet<int>();
        }

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
                    
                    if (string.IsNullOrEmpty(npc.FullName))
                        continue;

                    npcsToProcess.Add(i);
                    
                    Main.QueueMainThreadAction(() =>
                    {
                        try
                        {
                            Texture2D currentTexture;
                            
                            Main.instance.LoadNPC(npc.type);

                            if (TextureAssets.Npc[npc.type] == null)
                            {
                                Mod.Logger.Warn($"Texture not found for NPC: {npc.FullName}");
                                return;
                            }

                            currentTexture = TextureAssets.Npc[npc.type].Value;
                            
                            string base64Image = ExtractFirstFrame(currentTexture, npc);

                            var npcDict = new Dictionary<string, object>
                            {
                                {"name", npc.FullName},
                                {"id", npc.type},
                                {"image", base64Image}
                            };

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

                List<Dictionary<string, object>> listToUse;

                listToUse = _mainList.Values.ToList();

                if (max > listToUse.Count) {
                    warning = "MAX";
                    return JsonConvert.SerializeObject(warning);
                }

                _currentList = listToUse.Skip(Math.Max(0, max - 30)).Take(30).ToList();
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
