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

    public string LoadItemList(int max)
    {
        _currentList = new List<Dictionary<string, object>>();
        int remainingTasks = 30;
     
        for (int i = 0; i < 30; i++){
            
            var item = Main.recipe[i].createItem;
            


            Item new_item = new Item();
            new_item.SetDefaults(item.type);

            Asset<Texture2D> currentTexture;

            Main.QueueMainThreadAction(() => {
                Main.NewText("test");
                if (new_item.ModItem == null) {
                    currentTexture = ModContent.Request<Texture2D>($"Terraria/Images/Item_{new_item.type}");
                } else {
                    currentTexture = ModContent.Request<Texture2D>(new_item.ModItem.Texture);
                }

                while(!currentTexture.IsLoaded){
                    Thread.Yield();
                }
                
                string base64Image = ConvertTextureToBase64(currentTexture.Value);

                var itemDict = new Dictionary<string, object> // Create  dictionary for each item
                {
                    {"name", item.Name},
                    {"id", item.type}, // Item.type is the ID
                    {"image", base64Image}
                };

                
                Main.NewText("test2");

                lock (_currentList)
                {
                    _currentList.Add(itemDict);
                }

                Interlocked.Decrement(ref remainingTasks);
            });
        }

        while (remainingTasks > 0)
        {
            Thread.Sleep(10); // Allow other tasks to execute
        }
        
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
