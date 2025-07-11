using Terraria;
using Terraria.ModLoader;
using System.Collections.Generic;
using Terraria.ID;

namespace TerrariaCompanionMod
{
    public class UsePotions : ModSystem
    {
        public void consumePotions(Player player, List<PotionEntryData> potions)
        {
            foreach (var potionData in potions)
            {
                int itemType;

                if (potionData.mod == "Terraria")
                {
                    itemType = ItemID.Search.TryGetId(potionData.internalName, out int id) ? id : 0;
                }
                else
                {
                    Mod mod = ModLoader.GetMod(potionData.mod);
                    itemType = mod?.Find<ModItem>(potionData.internalName)?.Type ?? 0;
                }

                for (int i = 0; i < player.inventory.Length; i++)
                {
                    Item item = player.inventory[i];
                    if (item != null && item.type == itemType && item.stack > 0)
                    {
                        if (item.buffType > 0)
                        {
                            player.AddBuff(item.buffType, item.buffTime);
                        }

                        item.stack--;
                        if (item.stack <= 0)
                            player.inventory[i].TurnToAir();

                        break;
                    }
                }
            }
        }
    }
}
