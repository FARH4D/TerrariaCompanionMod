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


public class LoadItems : ModSystem
{
    private List<Dictionary<string, object>> _currentList;
    private Dictionary<string, List<Dictionary<string, object>>> _categorisedItems;    

    public override void OnWorldLoad()
    {   
        var storage = ItemStorage.Instance;

        var mainList = new Dictionary<int, Dictionary<string, object>>();

        try
        {
            for (int i = 0; i < Main.recipe.Length; i++)
            {
                if (Main.recipe[i] == null || Main.recipe[i].createItem == null)
                    continue;

                var item = Main.recipe[i].createItem;

                Mod.Logger.Info($"Found Item {i}: {item.Name}");

                Main.QueueMainThreadAction(() =>
                {
                    try
                    {
                        Item new_item = new Item();
                        new_item.SetDefaults(item.type);

                        var texturePath = new_item.ModItem?.Texture ?? $"Images/Item_{new_item.type}";

                        if (!ModContent.HasAsset(texturePath))
                            return; // Skip item if texture is missing

                        Texture2D currentTexture = ModContent.Request<Texture2D>(texturePath).Value;
                        string base64Image = ConvertTextureToBase64(currentTexture);

                        var itemDict = new Dictionary<string, object>
                        {
                            {"name", item.Name},
                            {"id", item.type},
                            {"image", base64Image}
                        };

                        mainList[item.type] = itemDict;
                        storage.CategoriseItem(itemDict, new_item);
                    }
                    catch (Exception innerEx)
                    {
                        Mod.Logger.Warn($"Error processing item texture: {innerEx}");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Mod.Logger.Warn($"Unexpected error in OnWorldLoad: {ex}");
        }

        storage.SetMainList(mainList);
    }



    public override void OnWorldUnload()
{
    var storage = ItemStorage.Instance;

    try
    {
        // Clear main list and remove any references to items or images
        storage.ClearMainList();


        Mod.Logger.Info("World unloaded and resources cleaned up.");
    }
    catch (Exception ex)
    {
        Mod.Logger.Warn($"Error cleaning up on world unload: {ex}");
    }
}

    

    public void categoriseItem(Dictionary<string, object> itemDict, Item new_item){
        if (new_item.damage > 0)
        {
            if (new_item.CountsAsClass(DamageClass.Melee) && !(new_item.pick > 0 || new_item.axe > 0 || new_item.hammer > 0))
                _categorisedItems["Melee"].Add(itemDict);
            else if (new_item.CountsAsClass(DamageClass.Ranged) && !(new_item.pick > 0 || new_item.axe > 0 || new_item.hammer > 0))
                _categorisedItems["Ranged"].Add(itemDict);
            else if (new_item.CountsAsClass(DamageClass.Magic) && !(new_item.pick > 0 || new_item.axe > 0 || new_item.hammer > 0))
                _categorisedItems["Mage"].Add(itemDict);
            else if (new_item.CountsAsClass(DamageClass.Summon) && !(new_item.pick > 0 || new_item.axe > 0 || new_item.hammer > 0))
                _categorisedItems["Summoner"].Add(itemDict);
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
                if (!categorisedItems.ContainsKey(category))
                    return "Error: Category not found!";

                listToUse = categorisedItems[category];
            }
            Main.NewText("jmmm");
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