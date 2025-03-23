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

namespace TerrariaCompanionMod
{
    public class ItemPage : ModSystem
    {
        List<List<Dictionary<string, object>>> allRecipes;
        bool addCrafting = false;

        public async Task<string> LoadData(int itemId)
        {
            return await Task.Run(() =>
            {
                Item item = new Item();
                item.SetDefaults(itemId);

                allRecipes = new List<List<Dictionary<string, object>>>();

                List<Task> mainThreadTasks = new List<Task>();
                
                for (int i = 0; i < Main.recipe.Length; i++)
                {
                    Recipe recipe = Main.recipe[i];

                    if (recipe != null && recipe.createItem.type == itemId)
                    {
                        // List of crafting stations needed for each recipe
                        List<Dictionary<string, object>> craftingStations = new List<Dictionary<string, object>>();
                        foreach (int tileId in recipe.requiredTile)
                        {
                            if (tileId != -1) // -1 means no station is needed
                            {
                                int stationItemId = GetStationItemId(tileId);
                                if (stationItemId == -1)
                                {
                                    craftingStations.Add(new Dictionary<string, object>
                                    {
                                        {"id", -1},
                                        {"name", Lang.GetMapObjectName(tileId)},
                                        {"image", null}
                                    });
                                }
                                else
                                {   
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
                                        else if (stationItem.ModItem != null)
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
                        }

                        List<Dictionary<string, object>> recipeList = new List<Dictionary<string, object>>();

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
                                    else if (new_item.ModItem != null)
                                    {
                                        var texturePath = new_item.ModItem.Texture;
                                        if (ModContent.HasAsset(texturePath))
                                        {
                                            currentTexture = ModContent.Request<Texture2D>(texturePath).Value;
                                        }
                                    }

                                    string base64Image = ConvertTextureToBase64(currentTexture);

                                    var itemDict = new Dictionary<string, object>
                                    {
                                        {"id", new_item.type},
                                        {"name", Lang.GetItemNameValue(new_item.type)},
                                        {"image", base64Image},
                                        {"quantity", new_item.stack}
                                    };
                                    
                                    if (!addCrafting)
                                    {
                                        foreach (Dictionary<string, object> station in craftingStations)
                                        {
                                            recipeList.Add(station);
                                        }
                                        addCrafting = true;
                                    }

                                    recipeList.Add(itemDict);
                                    tcs.SetResult(true);
                                });
                                mainThreadTasks.Add(tcs.Task);
                            }
                        }
                        allRecipes.Add(recipeList);
                    }
                }

                Task.WaitAll(mainThreadTasks.ToArray());

                var data = new
                {
                    name = item.Name,
                    recipes = allRecipes
                };

                return JsonConvert.SerializeObject(data);
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
