using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace TerrariaCompanionMod
{
    public class ItemStorage
    {
        private static ItemStorage _instance;
        public static ItemStorage Instance
        {
            get
            {
                if (_instance == null)
                    throw new System.Exception("ItemStorage has not been initialized. Call Init(Mod mod) first.");
                return _instance;
            }
        }
        private List<Dictionary<string, object>> _totalList;
        private List<Dictionary<string, object>> _vanillaList;
        private List<Dictionary<string, object>> _moddedList;
        private Dictionary<string, List<Dictionary<string, object>>> _categorisedItems;
        private Mod _mod;
    
        private ItemStorage(Mod mod)
        {
            
            _mod = mod;
            _totalList = new List<Dictionary<string, object>>();
            _vanillaList = new List<Dictionary<string, object>>();
            _moddedList = new List<Dictionary<string, object>>();
            _categorisedItems = new Dictionary<string, List<Dictionary<string, object>>>
            {
                {"melee", new List<Dictionary<string, object>>()},
                {"ranged", new List<Dictionary<string, object>>()},
                {"mage", new List<Dictionary<string, object>>()},
                {"summoner", new List<Dictionary<string, object>>()},
                {"bosssummon", new List<Dictionary<string, object>>()},
                {"Helmets", new List<Dictionary<string, object>>()},
                {"Chest", new List<Dictionary<string, object>>()},
                {"Legs", new List<Dictionary<string, object>>()},
                {"Accessories", new List<Dictionary<string, object>>()},
                {"pickaxe", new List<Dictionary<string, object>>()},
                {"Axes", new List<Dictionary<string, object>>()},
                {"Hammers", new List<Dictionary<string, object>>()},
                {"Consumables", new List<Dictionary<string, object>>()},
                {"Crafting Materials", new List<Dictionary<string, object>>()},
                {"Furniture & Decorations", new List<Dictionary<string, object>>()},
                {"Blocks", new List<Dictionary<string, object>>()},
                {"Miscellaneous", new List<Dictionary<string, object>>()}
            };
        }

        public List<Dictionary<string, object>> GetTotalList() => _totalList;
        public List<Dictionary<string, object>> getVanillaList() => _vanillaList;
        public List<Dictionary<string, object>> getModdedList() => _moddedList;

        public Dictionary<string, List<Dictionary<string, object>>> GetCategorisedItems() => _categorisedItems;

        public void SetTotalList(List<Dictionary<string, object>> newList) => _totalList = newList;
        public void SetVanillaList(List<Dictionary<string, object>> newList) => _vanillaList = newList;
        public void SetModdedList(List<Dictionary<string, object>> newList) => _moddedList = newList;

        public void CategoriseItem(Dictionary<string, object> itemDict, Item new_item)
        {
            if (new_item.damage > 0)
            {
                if (new_item.CountsAsClass(DamageClass.Melee) && !(new_item.pick > 0 || new_item.axe > 0 || new_item.hammer > 0)) {
                    _categorisedItems["melee"].Add(itemDict);
                }
                else if (new_item.CountsAsClass(DamageClass.Ranged) && !(new_item.pick > 0 || new_item.axe > 0 || new_item.hammer > 0))
                    _categorisedItems["ranged"].Add(itemDict);
                else if (new_item.CountsAsClass(DamageClass.Magic) && !(new_item.pick > 0 || new_item.axe > 0 || new_item.hammer > 0))
                    _categorisedItems["mage"].Add(itemDict);
                else if (new_item.CountsAsClass(DamageClass.Summon) && !(new_item.pick > 0 || new_item.axe > 0 || new_item.hammer > 0))
                    _categorisedItems["summoner"].Add(itemDict);
            }
        }

        public void ClearMainList()
        {
            _categorisedItems = new Dictionary<string, List<Dictionary<string, object>>> {
                {"melee", new List<Dictionary<string, object>>()},
                {"ranged", new List<Dictionary<string, object>>()},
                {"mage", new List<Dictionary<string, object>>()},
                {"summoner", new List<Dictionary<string, object>>()},
                {"bosssummon", new List<Dictionary<string, object>>()},
                {"Helmets", new List<Dictionary<string, object>>()},
                {"Chest", new List<Dictionary<string, object>>()},
                {"Legs", new List<Dictionary<string, object>>()},
                {"Accessories", new List<Dictionary<string, object>>()},
                {"pickaxe", new List<Dictionary<string, object>>()},
                {"Axes", new List<Dictionary<string, object>>()},
                {"Hammers", new List<Dictionary<string, object>>()},
                {"Consumables", new List<Dictionary<string, object>>()},
                {"Crafting Materials", new List<Dictionary<string, object>>()},
                {"Furniture & Decorations", new List<Dictionary<string, object>>()},
                {"Blocks", new List<Dictionary<string, object>>()},
                {"Miscellaneous", new List<Dictionary<string, object>>()}
            };
        }

        public static void Init(Mod mod)
        {
            if (_instance == null)
                _instance = new ItemStorage(mod);
        }

    }
}