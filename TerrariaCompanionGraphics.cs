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


public class MyGraphicsSystem : ModSystem
{
    public override void OnModLoad()
    {
        
    }

    public override void OnWorldLoad()
    {
        
    }
    

    private void SaveTextureToFile(Asset<Texture2D> textureAsset, string fileName)
        {
            try
            {
                Texture2D texture = textureAsset.Value;

                using (MemoryStream ms = new MemoryStream())
                {
                    texture.SaveAsPng(ms, texture.Width, texture.Height);

                    File.WriteAllBytes(fileName, ms.ToArray());

                    Console.WriteLine($"Texture saved as {fileName}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error saving texture: {e.Message}");
            }
}
}
