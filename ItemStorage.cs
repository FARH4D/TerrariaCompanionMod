using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

public class ItemStorage
{
    private static ItemStorage _instance;
    public static ItemStorage Instance => _instance ??= new ItemStorage();
    private Dictionary<int, Dictionary<string, object>> _mainList;
    private Dictionary<string, List<Dictionary<string, object>>> _categorisedItems;

    private ItemStorage()
    {
        _mainList = new Dictionary<int, Dictionary<string, object>>();
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

    public Dictionary<int, Dictionary<string, object>> GetMainList() => _mainList;

    public Dictionary<string, List<Dictionary<string, object>>> GetCategorisedItems() => _categorisedItems;

    public void SetMainList(Dictionary<int, Dictionary<string, object>> newList) => _mainList = newList;

    public void CategoriseItem(Dictionary<string, object> itemDict, Item new_item)
    {
        if (new_item.damage > 0)
        {
            if (new_item.CountsAsClass(DamageClass.Melee) && !(new_item.pick > 0 || new_item.axe > 0 || new_item.hammer > 0))
                _categorisedItems["Melee"].Add(itemDict);
            else if (new_item.CountsAsClass(DamageClass.Ranged) && !(new_item.pick > 0 || new_item.axe > 0 || new_item.hammer > 0))
                _categorisedItems["Ranged"].Add(itemDict);
            else if (new_item.CountsAsClass(DamageClass.Magic) && !(new_item.pick > 0 || new_item.axe > 0 || new_item.hammer > 0))
                _categorisedItems["Mage"].Add(itemDict);
            else if (new_item.CountsAsClass(DamageClass.Summon) && !(new_item.pick > 0 || new_item.axe > 0 || new_item.hammer > 0))
                _categorisedItems["Summoner"].Add(itemDict);
        }
    }

    public void ClearMainList()
    {
        _mainList.Clear();
        _categorisedItems.Clear();
    }
}