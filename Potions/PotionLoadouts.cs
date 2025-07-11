using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using System.IO;

namespace TerrariaCompanionMod
{
    public class PotionLoadouts : ModSystem
    {
        
        public async Task<string> GetConsumablesData(Player player)
        {
            return await Task.Run(() =>
            {
                var consumables = new Dictionary<string, object>();
                List<Task> mainThreadTasks = new List<Task>();

                foreach (var item in player.inventory)
                {
                    if (item == null || item.IsAir || !item.consumable || item.buffType <= 0)
                    continue;

                    string displayName = item.Name;
                    string modName = item.ModItem?.Mod?.Name ?? "Terraria";
                    string internalName = item.ModItem?.Name ?? GetVanillaInternalName(item.type);

                    var tcs = new TaskCompletionSource<bool>();
                    Main.QueueMainThreadAction(() =>
                    {
                        Texture2D texture = TextureAssets.Item[item.type]?.Value;
                        string base64 = "";

                        if (texture != null)
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                texture.SaveAsPng(ms, texture.Width, texture.Height);
                                base64 = Convert.ToBase64String(ms.ToArray());
                            }
                        }

                        consumables[displayName] = new
                        {
                            displayName,
                            modName,
                            internalName,
                            base64
                        };

                        tcs.SetResult(true);
                    });

                    mainThreadTasks.Add(tcs.Task);
                }

                Task.WaitAll(mainThreadTasks.ToArray());

                return JsonConvert.SerializeObject(consumables);
            });
        }
        private static string GetVanillaInternalName(int type)
        {
            return ItemID.Search.GetName(type) ?? "unknown";
        }
    }
}
