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
                {"throwing", new List<Dictionary<string, object>>()},
                {"bosssummon", new List<Dictionary<string, object>>()},
                {"helmets", new List<Dictionary<string, object>>()},
                {"body", new List<Dictionary<string, object>>()},
                {"legs", new List<Dictionary<string, object>>()},
                {"Accessories", new List<Dictionary<string, object>>()},
                {"pickaxe", new List<Dictionary<string, object>>()},
                {"Axes", new List<Dictionary<string, object>>()},
                {"Hammers", new List<Dictionary<string, object>>()},
                {"Consumables", new List<Dictionary<string, object>>()},
                {"ammo", new List<Dictionary<string, object>>()},
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

        public void SetTotalList()
        {
            _totalList.Clear();
            foreach (var category in _categorisedItems.Values)
            {
                _totalList.AddRange(category);
            }
        }

        public void CategoriseItem(Dictionary<string, object> itemDict, Item new_item)
        {
            if (new_item.damage > 0)
            {
                if (new_item.CountsAsClass(DamageClass.Melee) && !(new_item.pick > 0 || new_item.axe > 0 || new_item.hammer > 0)) {
                    _categorisedItems["melee"].Add(itemDict);
                }
                else if (new_item.CountsAsClass(DamageClass.Ranged)){
                    if (new_item.ammo == 0) {
                        _categorisedItems["ranged"].Add(itemDict);
                    }
                    else _categorisedItems["ammo"].Add(itemDict);
                }
                else if (new_item.CountsAsClass(DamageClass.Magic) && !(new_item.pick > 0 || new_item.axe > 0 || new_item.hammer > 0))
                    _categorisedItems["mage"].Add(itemDict);
                else if (new_item.CountsAsClass(DamageClass.Summon) && !(new_item.pick > 0 || new_item.axe > 0 || new_item.hammer > 0))
                    _categorisedItems["summoner"].Add(itemDict);
                else if (new_item.CountsAsClass(DamageClass.Throwing) && !(new_item.pick > 0 || new_item.axe > 0 || new_item.hammer > 0))
                    _categorisedItems["throwing"].Add(itemDict);
            }
            else if (new_item.headSlot != -1 || new_item.bodySlot != -1 || new_item.legSlot != -1)
            {
                if (new_item.headSlot !=-1) _categorisedItems["helmets"].Add(itemDict);
                else if (new_item.bodySlot !=-1) _categorisedItems["body"].Add(itemDict);
                else if (new_item.legSlot !=-1) _categorisedItems["legs"].Add(itemDict);
            }
        }

        public void ClearMainList()
        {
            _categorisedItems = new Dictionary<string, List<Dictionary<string, object>>> {
                {"melee", new List<Dictionary<string, object>>()},
                {"ranged", new List<Dictionary<string, object>>()},
                {"mage", new List<Dictionary<string, object>>()},
                {"summoner", new List<Dictionary<string, object>>()},
                {"throwing", new List<Dictionary<string, object>>()},
                {"bosssummon", new List<Dictionary<string, object>>()},
                {"helmets", new List<Dictionary<string, object>>()},
                {"body", new List<Dictionary<string, object>>()},
                {"legs", new List<Dictionary<string, object>>()},
                {"Accessories", new List<Dictionary<string, object>>()},
                {"pickaxe", new List<Dictionary<string, object>>()},
                {"Axes", new List<Dictionary<string, object>>()},
                {"Hammers", new List<Dictionary<string, object>>()},
                {"Consumables", new List<Dictionary<string, object>>()},
                {"ammo", new List<Dictionary<string, object>>()},
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