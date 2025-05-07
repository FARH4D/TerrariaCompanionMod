// Texture2D texture = textureAsset.Value;
            //                 // string filePath = Path.Combine(downloadsPath, $"npc{npcID}.png");
            //                 // int frameCount = Main.npcFrameCount[npcID]; // Get number of frames
            //                 // if (frameCount <= 0) frameCount = 1; // Ensure we don’t divide by 0

            //                 // int frameWidth = texture.Width;
            //                 // int frameHeight = texture.Height / frameCount;

            //                 // Texture2D firstFrameTexture = new Texture2D(Main.graphics.GraphicsDevice, frameWidth, frameHeight);
            //                 // Microsoft.Xna.Framework.Color[] fullPixels = new Microsoft.Xna.Framework.Color[texture.Width * texture.Height];
            //                 // texture.GetData(fullPixels);

            //                 // Microsoft.Xna.Framework.Color[] framePixels = new Microsoft.Xna.Framework.Color[frameWidth * frameHeight];
            //                 // for (int y = 0; y < frameHeight; y++)
            //                 // {
            //                 //     for (int x = 0; x < frameWidth; x++)
            //                 //     {
            //                 //         framePixels[y * frameWidth + x] = fullPixels[y * frameWidth + x];
            //                 //     }
            //                 // }

            //                 // firstFrameTexture.SetData(framePixels);