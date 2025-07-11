using System;
using Terraria;
using Terraria.ModLoader;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Terraria.Localization;


namespace TerrariaCompanionMod
{
    public class LoadChecklist : ModSystem
    {
        
        public async Task<string> LoadBosses()
        {
            return await Task.Run(() =>
            {   
                Mod bossChecklistMod = null;
                Mod terrariaCompanionMod = ModContent.GetInstance<TerrariaCompanionMod>();

                try {
                    bossChecklistMod = ModLoader.GetMod("BossChecklist");
                } catch (KeyNotFoundException) {
                    Main.NewText("no boss checklist");
                    return JsonConvert.SerializeObject("No BossChecklist");
                } catch (Exception ex) {
                    return JsonConvert.SerializeObject("Error checking for BossChecklist");
                }

                var bossList = bossChecklistMod.Call("GetBossInfoDictionary", terrariaCompanionMod) as Dictionary<string, Dictionary<string, object>>;

                List<Dictionary<string, object>> bossListData = new List<Dictionary<string, object>>();

                foreach (var kvp in bossList)
                {
                    var entryInfo = kvp.Value;
                    string bossName = string.Empty;

                    if (entryInfo.TryGetValue("displayName", out object displayNameObj) && displayNameObj is LocalizedText displayName)
                    {
                        bossName = displayName.Value;
                    }
                    else if (entryInfo.TryGetValue("key", out object keyObj) && keyObj is string keyName)
                    {
                        bossName = keyName;
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
                }

                string json = JsonConvert.SerializeObject(bossListData);
                return json;
            });
        }
    }
}