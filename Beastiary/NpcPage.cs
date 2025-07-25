using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Terraria.GameContent.ItemDropRules;
using System.Linq;

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

                drops = new List<Dictionary<string, object>>();

                List<IItemDropRule> dropRules = Main.ItemDropsDB.GetRulesForNPCID(npcId, false);

                List<DropRateInfo> allDropInfo = new List<DropRateInfo>();
                foreach (var rule in dropRules)
                {
                    DropRateInfoChainFeed feed = new DropRateInfoChainFeed(1f);
                    CollectDropsRecursive(rule, allDropInfo, feed);
                }

                List<Task> mainThreadTasks = new List<Task>();

                var groupedByItem = allDropInfo.GroupBy(info => info.itemId).Select(group =>
                {
                    var match = group.FirstOrDefault(info => MatchesCurrentDifficulty(info));
                    if (match.itemId == 0 && match.dropRate == 0f)
                        return group.First(); // fallback
                    return match;
                });

                foreach (var info in groupedByItem)
                {
                    var tcs = new TaskCompletionSource<bool>();
                    Main.QueueMainThreadAction(() =>
                    {
                        Item new_item = new Item();
                        new_item.SetDefaults(info.itemId);
                        Texture2D currentTexture = null;

                        if (new_item.ModItem == null)
                        {
                            if (TextureAssets.Item[new_item.type] != null)
                            {
                                Main.instance.LoadItem(new_item.type);
                                currentTexture = TextureAssets.Item[new_item.type].Value;
                            }
                        }
                        else
                        {
                            var texturePath = new_item.ModItem.Texture;
                            if (ModContent.HasAsset(texturePath))
                            {
                                currentTexture = ModContent.Request<Texture2D>(texturePath).Value;
                            }
                        }

                        string base64Image = ConvertTextureToBase64(currentTexture);

                        drops.Add(new Dictionary<string, object>
                        {
                            {"id", info.itemId},
                            {"name", Lang.GetItemNameValue(info.itemId)},
                            {"image", base64Image},
                            {"droprate", info.dropRate * 100}
                        });

                        tcs.SetResult(true);
                    });

                    mainThreadTasks.Add(tcs.Task);
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

                var data = new
                {
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

        private void CollectDropsRecursive(IItemDropRule rule, List<DropRateInfo> dropRateInfoList, DropRateInfoChainFeed chainFeed)
        {
            List<DropRateInfo> tempDrops = new List<DropRateInfo>();
            rule.ReportDroprates(tempDrops, chainFeed);

            dropRateInfoList.AddRange(tempDrops);

            foreach (var chain in rule.ChainedRules)
            {
                CollectDropsRecursive(chain.RuleToChain, dropRateInfoList, chainFeed);
            }
        }

        private bool MatchesCurrentDifficulty(DropRateInfo info)
        {
            if (info.conditions == null || info.conditions.Count == 0)
                return Main.GameMode == 0;

            foreach (var condition in info.conditions)
            {
                if (condition == null) continue;

                string name = condition.GetType().Name;

                if (Main.GameMode == 0 && name.Contains("Expert")) return false;
                if (Main.GameMode == 0 && name.Contains("Master")) return false;
                if (Main.GameMode == 1 && name.Contains("Master")) return false;
                if (Main.GameMode == 2 && !name.Contains("Master")) return false;
            }

            return true;
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
