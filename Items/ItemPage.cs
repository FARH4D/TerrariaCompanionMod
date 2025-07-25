using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Linq;
using Terraria.GameContent.ItemDropRules;
using Terraria.UI.Chat;
using Terraria.Localization;
using Terraria.UI;

namespace TerrariaCompanionMod
{
    public class ItemPage : ModSystem
    {
        List<List<Dictionary<string, object>>> allRecipes;
        bool addCrafting;

        public async Task<string> LoadData(int itemId)
        {
            return await Task.Run(() =>
            {
                addCrafting = false;
                Item item = new Item();
                item.SetDefaults(itemId);

                allRecipes = new List<List<Dictionary<string, object>>>();
                List<Task> mainThreadTasks = new();

                /////Recipes//////////////////////
                for (int i = 0; i < Main.recipe.Length; i++)
                {
                    Recipe recipe = Main.recipe[i];
                    List<Dictionary<string, object>> craftingStations = new();
                    if (recipe != null && recipe.createItem.type == itemId)
                    {
                        if (recipe.requiredTile.Count == 0)
                        {
                            Main.QueueMainThreadAction(() =>
                            {
                                craftingStations.Add(new Dictionary<string, object>
                                {
                                    {"id", 1},
                                    {"name", "None"},
                                    {"image", ""},
                                    {"quantity", 0}
                                });
                            });
                        }
                        else
                        {
                            foreach (int tileId in recipe.requiredTile)
                            {
                                int stationItemId = GetStationItemId(tileId);
                                Main.QueueMainThreadAction(() =>
                                {
                                    Texture2D currentTexture = null;
                                    Item stationItem = new Item();
                                    stationItem.SetDefaults(stationItemId);

                                    if (stationItem.ModItem == null)
                                    {
                                        if (TextureAssets.Item[stationItem.type] != null)
                                        {
                                            Main.instance.LoadItem(stationItem.type);
                                            currentTexture = TextureAssets.Item[stationItem.type].Value;
                                        }
                                    }
                                    else
                                    {
                                        var texturePath = stationItem.ModItem.Texture;
                                        if (ModContent.HasAsset(texturePath))
                                        {
                                            currentTexture = ModContent.Request<Texture2D>(texturePath).Value;
                                        }
                                    }

                                    string base64Image = ConvertTextureToBase64(currentTexture);

                                    craftingStations.Add(new Dictionary<string, object>
                                    {
                                        {"id", stationItemId},
                                        {"name", Lang.GetItemNameValue(stationItemId)},
                                        {"image", base64Image},
                                        {"quantity", 0}
                                    });
                                });
                            }
                        }

                        List<Dictionary<string, object>> recipeList = new();
                        foreach (Item new_item in recipe.requiredItem)
                        {
                            if (new_item != null && new_item.type != ItemID.None)
                            {
                                var tcs = new TaskCompletionSource<bool>();
                                Main.QueueMainThreadAction(() =>
                                {
                                    Texture2D currentTexture = null;

                                    if (new_item.ModItem == null)
                                    {
                                        if (TextureAssets.Item[new_item.type] != null)
                                        {
                                            Main.instance.LoadItem(new_item.type);
                                            currentTexture = TextureAssets.Item[new_item.type].Value;
                                        }
                                    }
                                    else
                                    {
                                        var texturePath = new_item.ModItem.Texture;
                                        if (ModContent.HasAsset(texturePath))
                                        {
                                            currentTexture = ModContent.Request<Texture2D>(texturePath).Value;
                                        }
                                    }

                                    string base64Image = ConvertTextureToBase64(currentTexture);

                                    if (!addCrafting)
                                    {
                                        foreach (Dictionary<string, object> station in craftingStations)
                                        {
                                            recipeList.Add(station);
                                        }
                                        addCrafting = true;
                                    }

                                    recipeList.Add(new Dictionary<string, object>
                                    {
                                        {"id", new_item.type},
                                        {"name", Lang.GetItemNameValue(new_item.type)},
                                        {"image", base64Image},
                                        {"quantity", new_item.stack}
                                    });

                                    tcs.SetResult(true);
                                });
                                mainThreadTasks.Add(tcs.Task);
                            }
                        }
                        allRecipes.Add(recipeList);
                    }
                }
                //////////////////////////////////////////////////////
                ///TOOLTIP///////////////////////////////////////////
                
                // List<string> tooltips = GetItemTooltips(item);

                ///////////////////////////////////////////////////// 
                ///////////NPC Drops/////////////////////////////////
                
                // List<Dictionary<string, object>> npcDrops = new();

                // for (int npcId = 0; npcId < NPCLoader.NPCCount; npcId++)
                // {
                //     List<IItemDropRule> dropRules;
                //     try
                //     {
                //         dropRules = Main.ItemDropsDB.GetRulesForNPCID(npcId, false);
                //     }
                //     catch
                //     {
                //         continue; // Skip NPCs with no drop rules or invalid ones
                //     }

                //     List<DropRateInfo> allDropInfo = new();
                //     foreach (var rule in dropRules)
                //     {
                //         DropRateInfoChainFeed feed = new(1f);
                //         CollectDropsRecursive(rule, allDropInfo, feed);
                //     }

                //     var candidates = allDropInfo.Where(info => info.itemId == itemId).ToList();
                //     if (candidates.Count == 0)
                //         continue;

                //     DropRateInfo matchingDrop = candidates.FirstOrDefault(MatchesCurrentDifficulty);

                //     if (!MatchesCurrentDifficulty(matchingDrop))
                //         matchingDrop = candidates[0];

                //     var tcs = new TaskCompletionSource<bool>();
                //     Main.QueueMainThreadAction(() =>
                //     {
                //         try
                //         {
                //             if (npcId < 0 || npcId >= NPCLoader.NPCCount)
                //                 return;

                //             NPC npc = new NPC();
                //             try
                //             {
                //                 npc.SetDefaults(npcId);
                //             }
                //             catch (Exception ex)
                //             {
                //                 Mod.Logger.Warn($"SetDefaults failed for NPC {npcId}: {ex.Message}");
                //                 return;
                //             }

                //             Texture2D currentTexture = null;
                //             if (npcId >= 0 && npcId < TextureAssets.Npc.Length && TextureAssets.Npc[npcId]?.IsLoaded == true)
                //             {
                //                 currentTexture = TextureAssets.Npc[npcId].Value;
                //             }

                //             string base64Image = currentTexture != null ? ConvertTextureToBase64(currentTexture) : "";

                //             string npcName = Lang.GetNPCNameValue(npcId);
                //             if (string.IsNullOrWhiteSpace(npcName)) npcName = $"NPC {npcId}";

                //             npcDrops.Add(new Dictionary<string, object>
                //             {
                //                 { "id", npcId },
                //                 { "name", npcName },
                //                 { "image", base64Image },
                //                 { "droprate", matchingDrop.dropRate * 100f }
                //             });
                //         }
                //         catch (Exception ex)
                //         {
                //             Mod.Logger.Warn($"Failed to load drop data for NPC {npcId}: {ex.Message}");
                //         }
                //         finally
                //         {
                //             tcs.SetResult(true);
                //         }
                //     });
                //     mainThreadTasks.Add(tcs.Task);
                // }

                Task.WaitAll(mainThreadTasks.ToArray());

                var data = new
                {
                    name = item.Name,
                    id = item.type,
                    recipes = allRecipes
                };

                return JsonConvert.SerializeObject(data);
            });
        }

        // public static List<string> GetItemTooltips(Item item)
        // {
        //     List<string> tooltipLines = new();

        //     if (item.ModItem != null)
        //     {
        //         List<TooltipLine> tooltips = new();
        //         item.ModItem.ModifyTooltips(tooltips);

        //         foreach (var line in tooltips)
        //         {
        //             if (!string.IsNullOrWhiteSpace(line.Text))
        //                 tooltipLines.Add(line.Text);
        //         }
        //     }
        //     else
        //     {
        //         int i = 0;
        //         while (true)
        //         {
        //             string key = $"ItemTooltip.{item.type}.{i}";
        //             string tooltip = Language.GetTextValue(key);

        //             if (string.IsNullOrWhiteSpace(tooltip) || tooltip == key)
        //                 break;

        //             tooltipLines.Add(tooltip);
        //             i++;
        //         }
        //     }
        //     return tooltipLines;
        // }

        private void CollectDropsRecursive(IItemDropRule rule, List<DropRateInfo> dropRateInfoList, DropRateInfoChainFeed chainFeed)
        {
            List<DropRateInfo> tempDrops = new List<DropRateInfo>();
            rule.ReportDroprates(tempDrops, chainFeed);

            dropRateInfoList.AddRange(tempDrops);

            foreach (var chain in rule.ChainedRules)
            {
                CollectDropsRecursive(chain.RuleToChain, dropRateInfoList, chainFeed);
            }
        }

        private bool MatchesCurrentDifficulty(DropRateInfo info)
        {
            if (info.conditions == null || info.conditions.Count == 0)
                return Main.GameMode == 0;

            foreach (var condition in info.conditions)
            {
                if (condition == null) continue;

                string name = condition.GetType().Name;

                if (Main.GameMode == 0 && name.Contains("Expert")) return false;
                if (Main.GameMode == 0 && name.Contains("Master")) return false;
                if (Main.GameMode == 1 && name.Contains("Master")) return false;
                if (Main.GameMode == 2 && !name.Contains("Master")) return false;
            }

            return true;
        }

        private string ConvertTextureToBase64(Texture2D texture)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                texture.SaveAsPng(ms, texture.Width, texture.Height);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        private int GetStationItemId(int tileId)
        {
            return Enumerable.Range(0, ItemLoader.ItemCount)
                .Select(id =>
                {
                    Item tempItem = new Item();
                    tempItem.SetDefaults(id);
                    return (tempItem.createTile == tileId) ? id : -1;
                })
                .FirstOrDefault(id => id != -1);
        }

    }
}
