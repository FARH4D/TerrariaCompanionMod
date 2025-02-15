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


public class LoadItems : ModSystem
{

    private List<Dictionary<string, object>> _currentList;

    public string LoadItemList(int max)
    {
        _currentList = new List<Dictionary<string, object>>();
        
        for (int i = 0; i < 50; i++){
            var item = Main.recipe[i].createItem;
            var itemDict = new Dictionary<string, object> // Create  dictionary for each item
            {
                {"name", item.Name},
                {"id", item.type} // Item.type is the ID
            };
        
            _currentList.Add(itemDict);

        }

        return JsonConvert.SerializeObject(_currentList);

    }

    
}
