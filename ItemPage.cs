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

    public async Task<string> LoadData(int itemId)
    {  
        return await Task.Run(async () =>
        {
            Item item = new Item();
            item.SetDefaults(itemId);

            allRecipes = new List<List<Dictionary<string, object>>>();

            List<Task<Dictionary<string, object>>> textureLoadingTasks = new List<Task<Dictionary<string, object>>>();

            for (int i = 0; i < Main.recipe.Length; i++)
            {
                Recipe recipe = Main.recipe[i];

                if (recipe != null && recipe.createItem.type == itemId)
                {
                    List<Dictionary<string, object>> recipeList = new List<Dictionary<string, object>>();

                    foreach (Item new_item in recipe.requiredItem)
                    {
                        if (new_item != null && new_item.type != ItemID.None)
                        {
                            var textureTask = Task.Run(() =>
                            {
                                Texture2D currentTexture = null;
                                Dictionary<string, object> itemData = new Dictionary<string, object>();

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

                                itemData.Add("id", new_item.type);
                                itemData.Add("name", Lang.GetItemNameValue(new_item.type));
                                itemData.Add("image", base64Image);
                                itemData.Add("quantity", new_item.stack);

                                return itemData;  // Return the data for later use
                            });

                            textureLoadingTasks.Add(textureTask);
                        }
                    }

                    var resultData = await Task.WhenAll(textureLoadingTasks);

                    foreach (var result in resultData)
                    {
                        recipeList.Add(result);
                    }

                    allRecipes.Add(recipeList);
                }
            }


            var data = new {
                name = item.Name
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
    }
}
