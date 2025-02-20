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
    private HashSet<int> itemsToProcess; // Tracks items that haven't had their textures converted to a bitmap yet
    


    public override void Load()
        {
            ItemStorage.Init(Mod);
            itemsToProcess = new HashSet<int>();
        }

        public override void PostSetupContent()
        {
            LoadAllItemTextures("vanilla");
        }

        public override void PostUpdatePlayers()
        {
            if (!Main.gameMenu && !hasLoaded) // Ensure it's only executed once
            {
                hasLoaded = true;
                Main.QueueMainThreadAction(() =>
                {
                    LoadAllItemTextures("modded");
                });
            }
        }

    public void LoadAllItemTextures(string type)
    {   
        var mainList = new Dictionary<int, Dictionary<string, object>>();
        var storage = ItemStorage.Instance;

        try
        {
            for (int i = 0; i < Main.recipe.Length; i++)
            {
                if (Main.recipe[i] == null || Main.recipe[i].createItem == null)
                    continue;

                var item = Main.recipe[i].createItem;
                itemsToProcess.Add(item.type); // Track items to process

                Main.QueueMainThreadAction(() =>
                {
                    try
                    {
                        Item new_item = new Item();
                        new_item.SetDefaults(item.type);

                        Texture2D currentTexture;

                        if (new_item.ModItem == null && type == "vanilla") // Vanilla item
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

                        mainList[item.type] = itemDict;

                        storage.CategoriseItem(itemDict, new_item);
                    }
                    catch (Exception innerEx)
                    {
                        Mod.Logger.Warn($"Error processing item texture: {innerEx}");
                    }

                    itemsToProcess.Remove(item.type); // Mark this item as processed

                });
            }
        }
        catch (Exception ex)
        {
            Mod.Logger.Warn($"Unexpected error in LoadAllItemTextures: {ex}");
        }
        storage.SetMainList(mainList);
    }

    public override void OnWorldUnload()
    {
        var storage = ItemStorage.Instance;

        try
        {   
            storage.ClearMainList(); // Clears the main list to free up some memory
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
            var _mainList = storage.GetMainList();

            if (_mainList == null || _mainList.Count == 0)
            {
                return "Error: No items found!";
            }

            List<Dictionary<string, object>> listToUse;

            if (category == "all")
            {
                listToUse = _mainList.Values.ToList();
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
}
}