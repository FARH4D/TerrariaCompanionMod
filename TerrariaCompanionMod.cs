using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using System.IO;
using Terraria.ModLoader;
using System.Drawing;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ObjectInteractions;
using Terraria.GameContent.ItemDropRules;
using Terraria.Localization;

namespace TerrariaCompanionMod
{
    public class TerrariaCompanionMod : Mod
    {
        public static TerrariaCompanionMod Instance;

        public override void Load()
        {
            Instance = this;
            ItemStorage.Init(this);
            base.Load();
        }
        
        public override void Unload()
        {
            base.Unload();
        }
    }
}