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
    public class NpcPage : ModSystem
    {
        List<Dictionary<string, object>> drops;

        public async Task<string> LoadData(int npcId)
        {  
            return await Task.Run(() =>
            {
                NPC npc = new NPC();
                npc.SetDefaults(npcId);
                
                List<IItemDropRule> dropRules = Main.ItemDropsDB.GetRulesForNPCID(npcId, false);

                drops = new List<Dictionary<string, object>>();

                List<Task> mainThreadTasks = new List<Task>();
                foreach (IItemDropRule rule in dropRules)
                {
                    List<DropRateInfo> dropInfo = new List<DropRateInfo>();
                    rule.ReportDroprates(dropInfo, new DropRateInfoChainFeed(1f)); // Extract actual item drop info
                    foreach (var info in dropInfo)
                    {
                        if (dropInfo.Count > 1 && Main.GameMode != 0) { // The first drop rate is normally for normal mode, so this skips it if the user is in expert/master mode and shows that instead.
                            continue;
                        }
                        
                        var tcs = new TaskCompletionSource<bool>();
                        Main.QueueMainThreadAction(() =>
                        {   
                            Item new_item = new Item();
                            new_item.SetDefaults(info.itemId);
                            Texture2D currentTexture = null;

                            if (new_item.ModItem == null) {
                                if (TextureAssets.Item[new_item.type] != null)
                                {
                                    Main.instance.LoadItem(new_item.type);
                                    currentTexture = TextureAssets.Item[new_item.type].Value;    
                                }           
                            } else if (new_item.ModItem != null) {
                                var texturePath = new_item.ModItem.Texture;
                                if (ModContent.HasAsset(texturePath))
                                {
                                    currentTexture = ModContent.Request<Texture2D>(texturePath).Value;
                                }
                            }

                            string base64Image = ConvertTextureToBase64(currentTexture);

                            var itemDict = new Dictionary<string, object>
                            {
                                {"id", info.itemId},
                                {"name", Lang.GetItemNameValue(info.itemId)},
                                {"image", base64Image},
                                {"droprate", info.dropRate * 100}
                            };

                            drops.Add(itemDict);
                            tcs.SetResult(true);
                        });
                        mainThreadTasks.Add(tcs.Task);
                    }
                }
                
                Task.WaitAll(mainThreadTasks.ToArray());

                float npcKnockback = npc.knockBackResist;

                string knockback_str = npcKnockback switch
                {
                    var x when x == 0 => "None",
                    var x when x <= 0.3f => "Low",
                    var x when x <= 0.7f => "Medium",
                    _ => "High"
                };
                

                var data = new {
                    name = npc.FullName,
                    hp = npc.lifeMax,
                    defense = npc.defense,
                    attack = npc.damage,
                    knockback = knockback_str,
                    drop_list = drops
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
