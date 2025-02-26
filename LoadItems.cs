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
    public class LoadItems : ModSystem
    {
        private List<Dictionary<string, object>> _currentList; 
        private bool hasLoaded = false;
        private HashSet<int> itemsToProcess;

        public override void Load()
        {
            ItemStorage.Init(Mod);
            itemsToProcess = new HashSet<int>();
        }

        public override void PostSetupContent()
        {
            LoadVanillaTextures();
        }

        public override void PostUpdatePlayers()
        {
            if (!Main.gameMenu && !hasLoaded) // Ensure its only executed once
            {
                hasLoaded = true;
                Main.QueueMainThreadAction(() =>
                {
                    LoadModdedTextures();
                });
            }
        }

        public void LoadVanillaTextures()
        {   
            var mainList = new List<Dictionary<string, object>>();
            var storage = ItemStorage.Instance;

            List<Task> tasks = new List<Task>();

            Main.QueueMainThreadAction(() =>
            {
                try
                {
                    for (int i = 0; i < Main.recipe.Length; i++)
                    {
                        if (Main.recipe[i] == null || Main.recipe[i].createItem == null)
                            continue;

                        var item = Main.recipe[i].createItem;
                        itemsToProcess.Add(item.type);
                        
                        if (item.type == ItemID.None)
                        {
                            continue;
                        }

                        tasks.Add(Task.Run(() =>
                        {   
                            try
                            {
                                Item new_item = new Item();
                                new_item.SetDefaults(item.type);

                                Texture2D currentTexture = null;

                                if (TextureAssets.Item[new_item.type] == null)
                                {
                                    Mod.Logger.Warn($"Texture not found for vanilla item: {new_item.Name}");
                                    return;
                                }
                                Main.instance.LoadItem(new_item.type);
                                currentTexture = TextureAssets.Item[new_item.type].Value;

                                Main.QueueMainThreadAction(() =>
                                {
                                    try
                                    {
                                        if (currentTexture != null)
                                        {
                                            string base64Image = ConvertTextureToBase64(currentTexture);

                                            var itemDict = new Dictionary<string, object>
                                            {
                                                {"name", new_item.Name},
                                                {"id", new_item.type},
                                                {"image", base64Image}
                                            };

                                            Mod.Logger.Info($"Adding item: {new_item.Name} (ID: {new_item.type})");

                                            mainList.Add(itemDict);

                                            Mod.Logger.Info($"Main list has {mainList.Count} items");

                                            storage.CategoriseItem(itemDict, new_item);
                                        }
                                    }
                                    catch (Exception innerEx)
                                    {
                                        Mod.Logger.Warn($"Error processing item texture on main thread: {innerEx}");
                                    }
                                    finally
                                    {
                                        itemsToProcess.Remove(item.type);
                                    }
                                });
                            }
                            catch (Exception innerEx)
                            {
                                Mod.Logger.Warn($"Error processing item texture: {innerEx}");
                            }
                        }));
                    }

                    // Wait for all tasks to complete
                    Task.WhenAll(tasks).ContinueWith(_ =>
                    {
                        storage.SetVanillaList(mainList);
                    });
                }
                catch (Exception ex)
                {
                    Mod.Logger.Warn($"Unexpected error in LoadVanillaTextures: {ex}");
                }
            });
        }

        public void LoadModdedTextures()
        {   
            var mainList = new List<Dictionary<string, object>>();
            var storage = ItemStorage.Instance;

            List<Task> tasks = new List<Task>();

            Main.QueueMainThreadAction(() =>
            {
                try
                {
                    for (int i = 0; i < Main.recipe.Length; i++)
                    {
                        if (Main.recipe[i] == null || Main.recipe[i].createItem == null)
                            continue;

                        var item = Main.recipe[i].createItem;
                        itemsToProcess.Add(item.type);
                        
                        if (item.type == ItemID.None)
                        {
                            continue;
                        }

                        tasks.Add(Task.Run(() =>
                        {   
                            try
                            {
                                Item new_item = new Item();
                                new_item.SetDefaults(item.type);

                                Texture2D currentTexture = null;

                                var texturePath = new_item.ModItem.Texture;

                                if (!ModContent.HasAsset(texturePath))
                                {
                                    Mod.Logger.Warn($"Missing texture for modded item: {new_item.Name}");
                                    return;
                                }
                                currentTexture = ModContent.Request<Texture2D>(texturePath).Value;

                                Main.QueueMainThreadAction(() =>
                                {
                                    try
                                    {
                                        if (currentTexture != null)
                                        {
                                            string base64Image = ConvertTextureToBase64(currentTexture);

                                            var itemDict = new Dictionary<string, object>
                                            {
                                                {"name", new_item.Name},
                                                {"id", new_item.type},
                                                {"image", base64Image}
                                            };

                                            Mod.Logger.Info($"Adding item: {new_item.Name} (ID: {new_item.type})");

                                            mainList.Add(itemDict);

                                            Mod.Logger.Info($"Main list has {mainList.Count} items");

                                            storage.CategoriseItem(itemDict, new_item);
                                        }
                                    }
                                    catch (Exception innerEx)
                                    {
                                        Mod.Logger.Warn($"Error processing item texture on main thread: {innerEx}");
                                    }
                                    finally
                                    {
                                        itemsToProcess.Remove(item.type);
                                    }
                                });
                            }
                            catch (Exception innerEx)
                            {
                                Mod.Logger.Warn($"Error processing item texture: {innerEx}");
                            }
                        }));
                    }

                    Task.WhenAll(tasks).ContinueWith(_ =>
                    {
                        storage.SetModdedList(mainList);
                        var tempList = storage.getVanillaList();
                        tempList.AddRange(storage.getModdedList());
                        storage.SetTotalList(tempList);
                    });
                }
                catch (Exception ex)
                {
                    Mod.Logger.Warn($"Unexpected error in LoadVanillaTextures: {ex}");
                }
            });
        }

        public override void OnWorldUnload()
        {
            var storage = ItemStorage.Instance;

            try
            {   
                storage.ClearMainList();
                hasLoaded = false;
                Mod.Logger.Info("World unloaded and lists cleaned up.");
            }
            catch (Exception ex)
            {
                Mod.Logger.Warn($"Error cleaning up on world unload: {ex}");
            }
        }

        public async Task<string> LoadItemList(int max, string category)
        {
            return await Task.Run(() =>
            {
                var storage = ItemStorage.Instance;
                var _mainList = storage.GetTotalList();

                if (_mainList == null || _mainList.Count == 0)
                {
                    return "Error: No items found!";
                }
                Main.NewText(_mainList.Count);
                List<Dictionary<string, object>> listToUse;

                if (category == "all")
                {
                    listToUse = _mainList.ToList();
                }
                else
                {
                    var categorisedItems = storage.GetCategorisedItems();

                    if (categorisedItems == null ){
                        return "Error: Empty List!";
                    }

                    if (!categorisedItems.ContainsKey(category.Trim()))
                        return "Error: Category not found!";

                    listToUse = categorisedItems[category.Trim()];
                }

                // listToUse = listToUse.OrderBy(item => item["id"]).ToList();
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
    }
}