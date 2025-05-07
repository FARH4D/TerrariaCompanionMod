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
using Terraria.GameContent.ItemDropRules;
using Terraria.Localization;

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

                var bossList = bossChecklistMod.Call("GetBossInfoDictionary", terrariaCompanionMod) as Dictionary<string, Dictionary<string, object>>;
                if (bossList == null)
                {
                    Main.NewText("Error: bossList is null.");
                    return "Error: No bosses received.";
                }

                int i = 0;

                foreach (var kvp in bossList)
                {
                    if (i == bossNum) {
                        var entryInfo = kvp.Value;
                        string bossName = string.Empty;
                        string spawnInfo = string.Empty; 
                        bool isDefeated = false;

                        if (entryInfo.TryGetValue("displayName", out object displayNameObj) && displayNameObj is LocalizedText displayName)
                        {
                            bossName = displayName.Value;
                        }

                        else if (entryInfo.TryGetValue("key", out object keyObj) && keyObj is string keyName)
                        {
                            bossName = keyName;
                        }

                        if (entryInfo.TryGetValue("spawnInfo", out object spawnInfoObj) && spawnInfoObj is Func<LocalizedText> spawnInfoFunc)
                        {
                            spawnInfo = spawnInfoFunc()?.Value ?? "";
                        }

                        if (entryInfo.TryGetValue("downed", out object downedObj) && downedObj is Func<bool> downedFunc)
                        {
                            isDefeated = downedFunc();
                        }


                        string base64Image = "";


                        List<Dictionary<string, object>> spawnItemList = new List<Dictionary<string, object>>();

                        if (entryInfo.TryGetValue("spawnItems", out object spawnItemsObj) && spawnItemsObj is List<int> spawnItems)
                        {
                            foreach (int itemID in spawnItems)
                            {
                                string itemName = Lang.GetItemName(itemID).Value;

                                Item item = new Item();
                                item.SetDefaults(itemID);

                                Main.QueueMainThreadAction(() =>
                                {
                                    Texture2D currentTexture = null;

                                    if (item.ModItem == null)
                                    {
                                        if (TextureAssets.Item[item.type] != null)
                                        {
                                            Main.instance.LoadItem(item.type);
                                            currentTexture = TextureAssets.Item[item.type].Value;
                                        }
                                    }
                                    else if (item.ModItem != null)
                                    {
                                        var texturePath = item.ModItem.Texture;
                                        if (ModContent.HasAsset(texturePath))
                                        {
                                            currentTexture = ModContent.Request<Texture2D>(texturePath).Value;
                                        }
                                    }
                                    string base64Image = ConvertTextureToBase64(currentTexture);

                                    var itemEntry = new Dictionary<string, object>
                                    {
                                        {"name", itemName},
                                        {"id", itemID},
                                        {"image", base64Image},
                                    };

                                    spawnItemList.Add(itemEntry);
                                });
                        }

                        if (entryInfo.TryGetValue("npcIDs", out object npcIDsObj) && npcIDsObj is List<int> npcIDs)
                        {
                            foreach (int npcID in npcIDs)
                            {
                                string npcName = Lang.GetNPCName(npcID).Value;
                                Main.NewText("NPC: " + npcName);
                            }
                        }

                        List<Dictionary<string, object>> dropsList = new List<Dictionary<string, object>>();

                        if (entryInfo.TryGetValue("dropRateInfo", out object dropInfoObj) && dropInfoObj is List<DropRateInfo> dropRateList)
                        {
                            foreach (DropRateInfo drop in dropRateList)
                            {
                                string itemName = Lang.GetItemName(drop.itemId).Value;
                                float dropRate = drop.dropRate * 100f;

                                Item item = new Item();
                                item.SetDefaults(drop.itemId);
                                Main.QueueMainThreadAction(() =>
                                {
                                    Texture2D currentTexture = null;

                                    if (item.ModItem == null)
                                    {
                                        if (TextureAssets.Item[item.type] != null)
                                        {
                                            Main.instance.LoadItem(item.type);
                                            currentTexture = TextureAssets.Item[item.type].Value;
                                        }
                                    }
                                    else if (item.ModItem != null)
                                    {
                                        var texturePath = item.ModItem.Texture;
                                        if (ModContent.HasAsset(texturePath))
                                        {
                                            currentTexture = ModContent.Request<Texture2D>(texturePath).Value;
                                        }
                                    }

                                    string base64Image = ConvertTextureToBase64(currentTexture);

                                    var dropEntry = new Dictionary<string, object>
                                    {
                                        {"id", drop.itemId},
                                        {"name", itemName},
                                        {"image", base64Image},
                                        {"dropRate", dropRate}
                                    };

                                    dropsList.Add(dropEntry);
                                });
                            }
                        }

                        var data = new
                        {
                            bossName = bossName,
                            bossImage = base64Image,
                            spawnItems = spawnItemList,
                            spawnInfo = spawnInfo,
                            status = isDefeated,
                            drops = dropsList
                        };

                        string json = JsonConvert.SerializeObject(data);
                        return json;
                    }
                }
                i++;
            }
            return "no boss";
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
    }
}