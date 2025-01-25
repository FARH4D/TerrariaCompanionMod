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
        // Perform graphics-related tasks on the main thread
        
    }


    
	// Your code here
    public override void OnWorldLoad()
    {
        
    }
    

    private void SaveTextureToFile(Asset<Texture2D> textureAsset, string fileName)
        {
            try
            {
                // Extract the actual Texture2D from the Asset<Texture2D>
                Texture2D texture = textureAsset.Value;

                // Get a memory stream to save the PNG to
                using (MemoryStream ms = new MemoryStream())
                {
                    // Save the texture as PNG to the memory stream
                    texture.SaveAsPng(ms, texture.Width, texture.Height);

                    // Write the PNG data to a file
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
