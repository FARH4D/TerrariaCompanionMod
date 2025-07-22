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

namespace TerrariaCompanionMod
{
    public class LoadItems : ModSystem
    {
        private List<Dictionary<string, object>> _currentList; 
        private bool hasLoaded = false;
        private string warning;

        public override void Load()
        {
            ItemStorage.Init(Mod);
        }

        public override void PostSetupContent()
        {
            LoadTextures("vanilla");
        }

        public override void PostUpdatePlayers()
        {
            if (!Main.gameMenu && !hasLoaded) // Ensure its only executed once
            {
                hasLoaded = true;
                Main.QueueMainThreadAction(() =>
                {   
                    LoadTextures("modded");

                    var storage = ItemStorage.Instance;
                    storage.SetTotalList(); // Moves this inside the callback
                });
            }
        }

        public void LoadTextures(string type)
        {   
            var mainList = new List<Dictionary<string, object>>();
            var storage = ItemStorage.Instance;

            List<Task> tasks = new List<Task>();

            Main.QueueMainThreadAction(() =>
            {
                try
                {
                    for (int i = 0; i < ItemLoader.ItemCount; i++)
                    {
                        Item item = new Item();
                        item.SetDefaults(i);
                        
                        if (item.type == ItemID.None || string.IsNullOrEmpty(item.Name))
                        {
                            continue;
                        }

                        tasks.Add(Task.Run(() =>
                        {  
                            Main.QueueMainThreadAction(() =>
                            {
                            try
                            {
                                Item new_item = new Item();
                                new_item.SetDefaults(item.type);

                                Texture2D currentTexture;

                                if (new_item.ModItem == null && type == "vanilla")
                                {
                                    if (TextureAssets.Item[new_item.type] == null)
                                    {
                                        Mod.Logger.Warn($"Texture not found for vanilla item: {new_item.Name}");
                                        return;
                                    }
                                    Main.instance.LoadItem(new_item.type);
                                    currentTexture = TextureAssets.Item[new_item.type].Value;
                                }
                                else if (type == "modded" && new_item.ModItem != null) 
                                {   
                                    var texturePath = new_item.ModItem.Texture;

                                    if (!ModContent.HasAsset(texturePath))
                                    {
                                        Mod.Logger.Warn($"Missing texture for modded item: {new_item.Name}");
                                        return; // Skip item if texture is missing
                                    }
                                    currentTexture = ModContent.Request<Texture2D>(texturePath).Value;
                                }
                                else
                                {
                                    return;
                                }

                                string base64Image = ConvertTextureToBase64(currentTexture);

                                var itemDict = new Dictionary<string, object>
                                {
                                    {"name", new_item.Name},
                                    {"id", new_item.type},
                                    {"image", base64Image}
                                };

                                mainList.Add(itemDict);
                                storage.CategoriseItem(itemDict, new_item);
                            }

                            catch (Exception innerEx)
                            {
                                Mod.Logger.Warn($"Error processing item texture on main thread: {innerEx}");
                            }
                            });
                        }));
                    }
                    Task.WhenAll(tasks).ContinueWith(_ =>
                    {
                        storage.SetTotalList();
                    });
                }
                catch (Exception ex)
                {
                    Mod.Logger.Warn($"Unexpected error in LoadVanillaTextures: {ex}");
                }
            });
        }

        public async Task<string> LoadItemList(int max, string category, string search)
        {
            return await Task.Run(() =>
            {
                var storage = ItemStorage.Instance;
                storage.SetTotalList();

                var _mainList = storage.GetTotalList();

                if (_mainList == null || _mainList.Count == 0)
                {
                    return "Error: No items found!";
                }

                List<Dictionary<string, object>> listToUse;

                if (category == "all")
                {
                    listToUse = _mainList.ToList();
                }
                else
                {
                    var categorisedItems = storage.GetCategorisedItems();

                    if (categorisedItems == null)
                    {
                        return "Error: Empty List!";
                    }

                    if (!categorisedItems.ContainsKey(category.Trim()))
                    {
                        return "Error: Category not found!";
                    }

                    listToUse = categorisedItems[category.Trim()];
                }

                search = search?.Trim();

                if (!string.IsNullOrEmpty(search))
                {
                    int beforeFilter = listToUse.Count;
                    listToUse = listToUse.Where(item => item.TryGetValue("name", out var nameObj) && nameObj is string name && name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                }

                if (max > listToUse.Count && max != 30)
                {
                    warning = "MAX";
                    return JsonConvert.SerializeObject(warning);
                }

                _currentList = listToUse.Skip(Math.Max(0, max - 30)).Take(30).ToList();

                return JsonConvert.SerializeObject(_currentList);
            });
        }

        public int[] ingredientNum(Player player, int trackedItemId)
        {
            Recipe[] recipes = Main.recipe; // Gets all the recipes for the tracked item
            Recipe matchedRecipe = null; 

            foreach (var recipe in recipes)
            {
                if (recipe.createItem != null && recipe.createItem.type == trackedItemId)
                {
                    matchedRecipe = recipe; // Currently takes only the first recipe (this will be changed eventually to account for multiple different recipes)
                    break;
                }
            }

            if (matchedRecipe == null)
            {
                return new int[0]; // In case there's no recipe found
            }

            List<int> result = new List<int>();

            foreach (var ingredient in matchedRecipe.requiredItem)
            {
                if (ingredient != null && ingredient.type != 0 && ingredient.stack > 0)
                {
                    int count = 0;
                    foreach (var item in player.inventory)
                    {
                        if (item != null && !item.IsAir && item.type == ingredient.type)
                        {
                            count += item.stack;
                        }
                    }
                    result.Add(count);
                }
            }
            return result.ToArray();
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