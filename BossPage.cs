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
using Terraria.Localization;

namespace TerrariaCompanionMod
{
    public class BossPage : ModSystem
    {
        int i = 0;

        public async Task<string> LoadData(int bossNum)
        {
            return await Task.Run(() =>
            {
                Mod bossChecklistMod = ModLoader.GetMod("BossChecklist");
                Mod terrariaCompanionMod = ModContent.GetInstance<TerrariaCompanionMod>();

                var bossList = bossChecklistMod.Call("GetBossInfoDictionary", terrariaCompanionMod) as Dictionary<string, Dictionary<string, object>>;

                List<Dictionary<string, object>> bossListData = new List<Dictionary<string, object>>();

                foreach (var kvp in bossList)
                {
                    if (i == bossNum) {
                        var entryInfo = kvp.Value;
                        string bossName = string.Empty;
                        string spawnInfo = string.Empty;

                        if (entryInfo.TryGetValue("displayName", out object displayNameObj) && displayNameObj is LocalizedText displayName)
                        {
                            bossName = displayName.Value;
                        }

                        else if (entryInfo.TryGetValue("key", out object keyObj) && keyObj is string keyName)
                        {
                            bossName = keyName;
                        }

                        if (entryInfo.TryGetValue("spawnInfo", out object spawnInfoObj) && spawnInfoObj is LocalizedText spawnInfo2)
                        {
                            spawnInfo = spawnInfo2.Value;
                            Main.NewText("success");
                            Main.NewText(spawnInfo);
                        }

                        if (entryInfo.TryGetValue("downed", out object downedObj) && downedObj is Func<bool> downedFunc)
                        {
                            bool isDefeated = downedFunc();
                            var bossData = new Dictionary<string, object>
                            {
                                { "name", bossName },
                                { "downed", isDefeated }
                            };
                            bossListData.Add(bossData);
                        }
                        string json = JsonConvert.SerializeObject(bossListData);
                        return json;
                    }
                    i++;
                }
                return null;
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
