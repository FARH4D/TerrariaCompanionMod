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
        bool addCrafting;

        public async Task<string> LoadData(int itemId)
        {
            return await Task.Run(() =>
            {
                addCrafting = false;
                Item item = new Item();
                item.SetDefaults(itemId);

                allRecipes = new List<List<Dictionary<string, object>>>();

                List<Task> mainThreadTasks = new List<Task>();
                
                for (int i = 0; i < Main.recipe.Length; i++)
                {
                    Recipe recipe = Main.recipe[i];
                    List<Dictionary<string, object>> craftingStations = new List<Dictionary<string, object>>();
                    if (recipe != null && recipe.createItem.type == itemId)
                    {
                        if (recipe.requiredTile.Count == 0) {
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
