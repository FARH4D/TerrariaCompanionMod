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





        public void LoadData(int npcId)
        {
            NPC npc = new NPC();
            npc.SetDefaults(npcId);
            
            
            List<IItemDropRule> dropRules = Main.ItemDropsDB.GetRulesForNPCID(npcId, false);

            foreach (IItemDropRule rule in dropRules)
            {
                List<DropRateInfo> dropInfo = new List<DropRateInfo>();
                rule.ReportDroprates(dropInfo, new DropRateInfoChainFeed(1f)); // Extract actual item drop info

                if (dropInfo.Count > 1) {
                    foreach (var info in dropInfo)
                    {
                        Main.NewText($"Possible drop from {npc.FullName}: {info.itemId} ({Lang.GetItemNameValue(info.itemId)}) - Drop Rate: {info.dropRate * 100}%");
                    }
                }
            }
            Main.NewText(npc.lifeMax);
            Main.NewText(npc.defense);
        }

    }
}
