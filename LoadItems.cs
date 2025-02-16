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


public class LoadItems : ModSystem
{
    private List<Dictionary<string, object>> _currentList;
    private Dictionary<string, List<Dictionary<string, object>>> _categorisedItems;    

public override void PostSetupContent()
{

        _categorisedItems = new Dictionary<string, List<Dictionary<string, object>>>
    {
        {"Melee", new List<Dictionary<string, object>>()},
        {"Ranged", new List<Dictionary<string, object>>()},
        {"Mage", new List<Dictionary<string, object>>()},
        {"Summoner", new List<Dictionary<string, object>>()},
        {"Helmets", new List<Dictionary<string, object>>()},
        {"Chest", new List<Dictionary<string, object>>()},
        {"Legs", new List<Dictionary<string, object>>()},
        {"Accessories", new List<Dictionary<string, object>>()},
        {"Pickaxes", new List<Dictionary<string, object>>()},
        {"Axes", new List<Dictionary<string, object>>()},
        {"Hammers", new List<Dictionary<string, object>>()},
        {"Consumables", new List<Dictionary<string, object>>()},
        {"Crafting Materials", new List<Dictionary<string, object>>()},
        {"Furniture & Decorations", new List<Dictionary<string, object>>()},
        {"Blocks", new List<Dictionary<string, object>>()},
        {"Miscellaneous", new List<Dictionary<string, object>>()}
    };

    try
    {
        for (int i = 0; i < Main.recipe.Length; i++)
        {

            if (Main.recipe[i] == null || Main.recipe[i].createItem == null)
                continue;

            
            var item = Main.recipe[i].createItem;

            Main.QueueMainThreadAction(() =>
            {
                try
                {
                    Item new_item = new Item();
                    new_item.SetDefaults(item.type);

                    var texturePath = new_item.ModItem?.Texture ?? $"Terraria/Images/Item_{new_item.type}";

                    Texture2D currentTexture;

                    try
                    {
                        currentTexture = ModContent.Request<Texture2D>(texturePath).Value;
                    }
                    catch (Exception texEx)
                    {
                        return;
                    }

                    string base64Image = ConvertTextureToBase64(currentTexture);

                    var itemDict = new Dictionary<string, object>
                    {
                        {"name", item.Name},
                        {"id", item.type},
                        {"image", base64Image}
                    };

                    categoriseItem(itemDict, new_item);

                }
                catch (Exception innerEx)
                {
                    Mod.Logger.Warn($"Error processing item texture: {innerEx}");
                }

            });
        }
    }
    catch (IndexOutOfRangeException ex)
    {
        Mod.Logger.Warn($"Index out of bounds in PostSetupContent: {ex}");
    }
    catch (Exception ex)
    {
        Mod.Logger.Warn($"Unexpected error in PostSetupContent: {ex}");
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


    public async Task<string> LoadItemList(int max)
    {
        _currentList = new List<Dictionary<string, object>>();
        int remainingTasks = 30;
        int min = 0;
        
        if (max != 0)
            {
                min = max - 30;
            }
        
        List<Task> tasks = new List<Task>();

        for (int i = min; i < max; i++)
        {
            int index = i;

            tasks.Add(Task.Run(async () =>
            {
                var item = Main.recipe[index]?.createItem;

                if (item == null)
                {
                    return;
                }

                Item new_item = new Item();
                new_item.SetDefaults(item.type);

                var currentTexture = await Task.Run(() =>
                {
                    return ModContent.Request<Texture2D>(new_item.ModItem?.Texture ?? $"Terraria/Images/Item_{new_item.type}");
                });

                while (!currentTexture.IsLoaded)
                {
                    await Task.Delay(10);
                }

                string base64Image = null;
                await Task.Run(() =>
                {
                    Main.QueueMainThreadAction(() =>
                    {
                        base64Image = ConvertTextureToBase64(currentTexture.Value);
                    });
                });

                while (base64Image == null)
                {
                    await Task.Delay(10);
                }

                var itemDict = new Dictionary<string, object>
                {
                    {"name", item.Name},
                    {"id", item.type},
                    {"image", base64Image}
                };

                lock (_currentList)
                {
                    _currentList.Add(itemDict);
                }

                Interlocked.Decrement(ref remainingTasks);
            }));
        }

        // Wait for all tasks to finish
        await Task.WhenAll(tasks);

        return JsonConvert.SerializeObject(_currentList);
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