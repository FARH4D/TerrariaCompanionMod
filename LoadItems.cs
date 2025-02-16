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
                    Main.NewText($"Failed to load item {index}");
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