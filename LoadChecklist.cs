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


namespace TerrariaCompanionMod
{
    public class LoadChecklist : ModSystem
    {
        
        public async Task<string> LoadBosses()
        {
            return await Task.Run(() =>
            {   
                Mod bossChecklistMod = ModLoader.GetMod("BossChecklist");
                var bossList = bossChecklistMod.Call("GetBossInfoDictionary", this) as Dictionary<string, Dictionary<string, object>>;

                List<string> boss_list_names = new List<string>();

                return "hello";
                // return JsonConvert.SerializeObject(bossList);
            });
        }
    }
}