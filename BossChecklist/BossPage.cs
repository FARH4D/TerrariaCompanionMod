using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.Localization;
using Terraria.ModLoader;

namespace TerrariaCompanionMod
{
    public class BossPage : ModSystem
    {
        public async Task<string> LoadData(int bossNum)
        {
            return await Task.Run(() =>
            {
                Mod bossChecklistMod = ModLoader.GetMod("BossChecklist");
                Mod terrariaCompanionMod = ModContent.GetInstance<TerrariaCompanionMod>();

                var bossList = bossChecklistMod?.Call("GetBossInfoDictionary", terrariaCompanionMod) as Dictionary<string, Dictionary<string, object>>;
                if (bossList == null)
                    return JsonConvert.SerializeObject(new { error = "No bosses received" });

                var entry = bossList.ElementAtOrDefault(bossNum);
                if (entry.Value == null)
                    return JsonConvert.SerializeObject(new { error = "Boss not found" });

                var entryInfo = entry.Value;
                string bossName = "";
                string spawnInfo = "";
                bool isDefeated = false;
                int npcID = -1;
                string bossTextureBase64 = "";
                List<Dictionary<string, object>> spawnItemList = new();
                List<Dictionary<string, object>> dropList = new();
                List<Task> mainThreadTasks = new();

                if (entryInfo.TryGetValue("displayName", out object displayNameObj) && displayNameObj is LocalizedText displayName)
                    bossName = displayName.Value;

                else if (entryInfo.TryGetValue("key", out object keyObj) && keyObj is string key)
                    bossName = key;

                if (entryInfo.TryGetValue("spawnInfo", out object spawnInfoObj) && spawnInfoObj is Func<LocalizedText> spawnInfoFunc)
                {
                    spawnInfo = spawnInfoFunc()?.Value ?? "";

                    if (!string.IsNullOrEmpty(spawnInfo))
                    {
                        var matches = System.Text.RegularExpressions.Regex.Matches(spawnInfo, @"\[i:([^\]]+)\]");
                        List<string> images = new();

                        foreach (System.Text.RegularExpressions.Match match in matches)
                        {
                            string rawId = match.Groups[1].Value;
                            int itemId = -1;

                            if (int.TryParse(rawId, out int vanillaId))
                            {
                                itemId = vanillaId;
                            }
                            else if (rawId.Contains('/'))
                            {
                                var split = rawId.Split('/');
                                if (split.Length == 2)
                                {
                                    string itemModName = split[0];
                                    string itemName = split[1];

                                    Mod mod = ModLoader.GetMod(itemModName);
                                    if (mod != null && mod.TryFind(itemName, out ModItem modItem))
                                    {
                                        itemId = modItem.Type;
                                    }
                                }
                            }

                            if (itemId >= 0)
                            {
                                string itemName = Lang.GetItemName(itemId).Value;
                                string imageBase64 = "";

                                var task = new TaskCompletionSource<bool>();
                                Main.QueueMainThreadAction(() =>
                                {
                                    try
                                    {
                                        Item item = new Item();
                                        item.SetDefaults(itemId);
                                        Texture2D texture = null;

                                        if (item.ModItem == null)
                                        {
                                            if (TextureAssets.Item[item.type]?.IsLoaded == true)
                                            {
                                                Main.instance.LoadItem(item.type);
                                                texture = TextureAssets.Item[item.type].Value;
                                            }
                                        }
                                        else
                                        {
                                            string texPath = item.ModItem.Texture;
                                            if (ModContent.HasAsset(texPath))
                                                texture = ModContent.Request<Texture2D>(texPath).Value;
                                        }

                                        if (texture != null)
                                            imageBase64 = ConvertTextureToBase64(texture);

                                        task.SetResult(true);
                                    }
                                    catch (Exception ex)
                                    {
                                        task.SetResult(true);
                                    }
                                });

                                task.Task.Wait();

                                spawnInfo = spawnInfo.Replace(match.Value, itemName);

                                if (!string.IsNullOrEmpty(imageBase64))
                                    images.Add($":{imageBase64}:");
                            }
                        }

                        if (images.Count > 0)
                        {
                            spawnInfo += " " + string.Join(" ", images);
                        }
                    }
                }

                if (entryInfo.TryGetValue("downed", out object downedObj) && downedObj is Func<bool> downedFunc)
                    isDefeated = downedFunc();

                if (entryInfo.TryGetValue("npcIDs", out object npcIDsObj) && npcIDsObj is List<int> npcIDs && npcIDs.Count > 0)
                    npcID = npcIDs[0];

                string modName = "Terraria";
                if (entryInfo.TryGetValue("modSource", out object modSourceObj) && modSourceObj is string modSourceStr)
                {
                    modName = modSourceStr;
                }

                string assetPath = $"BossChecklist/Resources/BossTextures/Boss{npcID}";

                if (entryInfo.TryGetValue("spawnItems", out object spawnItemsObj) && spawnItemsObj is List<int> spawnItems)
                {
                    foreach (int itemID in spawnItems)
                    {
                        var task = new TaskCompletionSource<bool>();
                        Main.QueueMainThreadAction(() =>
                        {
                            try
                            {
                                Item item = new Item();
                                item.SetDefaults(itemID);
                                Texture2D texture = null;

                                if (item.ModItem == null)
                                {
                                    if (TextureAssets.Item[item.type]?.IsLoaded == true)
                                    {
                                        Main.instance.LoadItem(item.type);
                                        texture = TextureAssets.Item[item.type].Value;
                                    }
                                }
                                else
                                {
                                    string texPath = item.ModItem.Texture;
                                    if (ModContent.HasAsset(texPath))
                                        texture = ModContent.Request<Texture2D>(texPath).Value;
                                }

                                string image = ConvertTextureToBase64(texture);

                                spawnItemList.Add(new Dictionary<string, object>
                                {
                                {"name", Lang.GetItemName(itemID).Value },
                                {"id", itemID },
                                {"image", image }
                                });

                                task.SetResult(true);
                            }
                            catch (Exception ex)
                            {
                                task.SetException(ex);
                            }
                        });
                        mainThreadTasks.Add(task.Task);
                    }
                }

                var dropRules = Main.ItemDropsDB.GetRulesForNPCID(npcID, false);
                foreach (var rule in dropRules)
                {
                    var dropRateInfo = new List<DropRateInfo>();
                    rule.ReportDroprates(dropRateInfo, new DropRateInfoChainFeed(1f));

                    foreach (var info in dropRateInfo)
                    {
                        if (dropRateInfo.Count > 1 && Main.GameMode != 0)
                            continue;

                        var task = new TaskCompletionSource<bool>();
                        Main.QueueMainThreadAction(() =>
                        {
                            try
                            {
                                Item item = new Item();
                                item.SetDefaults(info.itemId);
                                Texture2D texture = null;

                                if (item.ModItem == null)
                                {
                                    if (TextureAssets.Item[item.type]?.IsLoaded == true)
                                    {
                                        Main.instance.LoadItem(item.type);
                                        texture = TextureAssets.Item[item.type].Value;
                                    }
                                }
                                else
                                {
                                    string texPath = item.ModItem.Texture;
                                    if (ModContent.HasAsset(texPath))
                                        texture = ModContent.Request<Texture2D>(texPath).Value;
                                }

                                string image = ConvertTextureToBase64(texture);

                                dropList.Add(new Dictionary<string, object>
                                {
                                {"id", info.itemId },
                                {"name", Lang.GetItemNameValue(info.itemId) },
                                {"image", image },
                                {"droprate", info.dropRate > 0f ? info.dropRate * 100f : 0f }
                                });

                                task.SetResult(true);
                            }
                            catch (Exception ex)
                            {
                                task.SetException(ex);
                            }
                        });
                        mainThreadTasks.Add(task.Task);
                    }
                }

                var bossTextureTask = new TaskCompletionSource<bool>();

                Main.QueueMainThreadAction(() =>
                {
                    try
                    {
                        Texture2D texture = null;
                        bool success = false;

                        if (ModContent.HasAsset(assetPath))
                        {
                            texture = ModContent.Request<Texture2D>(assetPath).Value;
                            bossTextureBase64 = ConvertTextureToBase64(texture);
                            success = true;
                        }
                        else
                        {
                            if (modName != "Terraria")
                            {
                                string nameNoSpaces = bossName.Replace(" ", "");
                                string basePathNoExt = $"{modName}/NPCs/{nameNoSpaces}/{nameNoSpaces}";
                                string basePath = basePathNoExt;
                                string altPath = basePathNoExt + "_BossChecklist";

                                if (ModContent.HasAsset(basePath))
                                {
                                    texture = ModContent.Request<Texture2D>(basePath, AssetRequestMode.ImmediateLoad).Value;
                                    bossTextureBase64 = ConvertTextureToBase64(texture);
                                    success = true;
                                }
                                else
                                {
                                    if (ModContent.HasAsset(altPath))
                                    {
                                        texture = ModContent.Request<Texture2D>(altPath, AssetRequestMode.ImmediateLoad).Value;
                                        bossTextureBase64 = ConvertTextureToBase64(texture);
                                        success = true;
                                    }
                                }
                            }
                        }
                        if (!success)
                        {
                            Main.instance.LoadNPC(npcID);

                            if (TextureAssets.Npc[npcID]?.IsLoaded == true)
                            {
                                Texture2D npcTexture = TextureAssets.Npc[npcID].Value;
                                NPC npc = new NPC();
                                npc.SetDefaults(npcID);
                                bossTextureBase64 = ExtractFirstFrame(npcTexture, npc);
                            }
                        }

                        bossTextureTask.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        bossTextureTask.SetException(ex);
                    }
                });

                Task.WaitAll(mainThreadTasks.ToArray());
                bossTextureTask.Task.Wait();

                var result = new
                {
                    bossName,
                    bossImage = bossTextureBase64,
                    spawnItems = spawnItemList,
                    spawnInfo,
                    status = isDefeated,
                    drops = dropList
                };

                return JsonConvert.SerializeObject(result);
            });
        }

        private string ConvertTextureToBase64(Texture2D texture)
        {
            using MemoryStream ms = new MemoryStream();
            texture.SaveAsPng(ms, texture.Width, texture.Height);
            return Convert.ToBase64String(ms.ToArray());
        }

        private string ExtractFirstFrame(Texture2D texture, NPC npc)
        {
            int frameCount = Main.npcFrameCount[npc.type];
            if (frameCount <= 0) frameCount = 1;

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