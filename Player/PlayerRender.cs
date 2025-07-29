using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using System.Collections.Generic;
using System.Threading.Tasks;
using Terraria.GameContent;
using Terraria.ID;
using ReLogic.Content;

namespace TerrariaCompanionMod
{
    public class PlayerRender : ModSystem
    {
        public static Dictionary<string, string> GetPlayerVisualBase64(Player player)
        {
            Dictionary<string, string> visualData = new();

            TaskCompletionSource<bool> tcs = new();
            Main.QueueMainThreadAction(() =>
            {
                try
                {
                    if (player.head > 0 && TextureAssets.ArmorHead[player.head]?.IsLoaded == true)
                    {
                        Texture2D headTex = TextureAssets.ArmorHead[player.head].Value;
                        visualData["HeadArmour"] = ExtractFirstFrame(headTex, 20);
                    }

                    int bodySlot = player.body;

                    if (bodySlot > 0 && bodySlot < TextureAssets.ArmorBody.Length)
                    {
                        string bodyPath = $"Terraria/Images/Armor/Armor_{bodySlot}";
                        Texture2D bodyTex = ModContent.Request<Texture2D>(bodyPath, AssetRequestMode.ImmediateLoad).Value;

                        visualData["BodyArmourTorso"] = ExtractFrameFromGrid(bodyTex, 9, 4, 1, 2);
                        visualData["BodyArmourLeftArm"] = ExtractFrameFromGrid(bodyTex, 9, 4, 2, 0);
                        visualData["BodyArmourRightArm"] = ExtractFrameFromGrid(bodyTex, 9, 4, 2, 2);
                        visualData["BodyArmourLeftShoulder"] = ExtractFrameFromGrid(bodyTex, 9, 4, 0, 3);
                    }

                    if (player.legs > 0 && TextureAssets.ArmorLeg[player.legs]?.IsLoaded == true)
                    {
                        Texture2D legTex = TextureAssets.ArmorLeg[player.legs].Value;
                        visualData["LegArmour"] = ExtractFirstFrame(legTex, 20);
                    }

                    int headSlot = player.head;

                    string playerEyes1 = $"Terraria/Images/Player_0_1"; // Gets the white eye texture to stitch it on top of the player model
                    Texture2D playerEyesTex1 = ModContent.Request<Texture2D>(playerEyes1, AssetRequestMode.ImmediateLoad).Value;
                    visualData["Eyes1"] = ExtractFirstFrame(playerEyesTex1, 20);

                    string playerEyes2 = $"Terraria/Images/Player_0_2"; // Gets the 'pupil' eye texture to stitch it on top of the player model (so it can be coloured)
                    Texture2D playerEyesTex2 = ModContent.Request<Texture2D>(playerEyes2, AssetRequestMode.ImmediateLoad).Value;
                    visualData["Eyes2"] = ExtractFirstFrame(playerEyesTex2, 20);

                    if (headSlot < 0)
                    {
                        string hairTexturePath = $"Terraria/Images/Player_Hair_{player.hair + 1}"; // If the player has no helmet equipped, shows hair as normal
                        Texture2D hairTex = ModContent.Request<Texture2D>(hairTexturePath, AssetRequestMode.ImmediateLoad).Value;
                        visualData["Hair"] = ExtractFirstFrame(hairTex, 14);
                    }
                    else
                    {
                        bool drawFullHair = ArmorIDs.Head.Sets.DrawFullHair[headSlot]; // Gets values of whether hair should be normal or hat hair version
                        bool drawHatHair = ArmorIDs.Head.Sets.DrawHatHair[headSlot];

                        if (drawFullHair)
                        {
                            string hairTexturePath = $"Terraria/Images/Player_Hair_{player.hair + 1}";
                            Texture2D hairTex = ModContent.Request<Texture2D>(hairTexturePath, AssetRequestMode.ImmediateLoad).Value;
                            visualData["Hair"] = ExtractFirstFrame(hairTex, 14);
                        }
                        else if (drawHatHair)
                        {
                            string hatHairTexturePath = $"Terraria/Images/Player_HairAlt_{player.hair + 1}";
                            Texture2D hatHairTex = ModContent.Request<Texture2D>(hatHairTexturePath, AssetRequestMode.ImmediateLoad).Value;
                            visualData["Hair"] = ExtractFirstFrame(hatHairTex, 14);
                        }
                        else
                        {
                            visualData["Hair"] = ""; // Returns nothing so the hair just doesn't render on the app (tbh just too lazy to add a proper check for the app)
                        }
                    }

                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetResult(true);
                }
            });

            tcs.Task.Wait();

            visualData["SkinColor"] = ColorToHex(player.skinColor);
            visualData["HairColor"] = ColorToHex(player.hairColor);
            visualData["EyeColor"] = ColorToHex(player.eyeColor);

            return visualData;
        }

        private static string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private static string ConvertTextureToBase64(Texture2D texture)
        {
            using MemoryStream ms = new();
            texture.SaveAsPng(ms, texture.Width, texture.Height);
            return Convert.ToBase64String(ms.ToArray());
        }

        private static string ExtractFirstFrame(Texture2D texture, int frameCount = 1)
        {
            if (texture == null) return "";

            int frameWidth = texture.Width;
            int frameHeight = texture.Height / frameCount;

            Texture2D firstFrameTexture = new Texture2D(Main.graphics.GraphicsDevice, frameWidth, frameHeight);
            Color[] fullPixels = new Color[texture.Width * texture.Height];
            texture.GetData(fullPixels);

            Color[] framePixels = new Color[frameWidth * frameHeight];
            for (int y = 0; y < frameHeight; y++)
            {
                for (int x = 0; x < frameWidth; x++)
                {
                    framePixels[y * frameWidth + x] = fullPixels[y * frameWidth + x];
                }
            }

            firstFrameTexture.SetData(framePixels);
            return ConvertTextureToBase64(firstFrameTexture);
        }

        private static string ExtractFrameFromGrid(Texture2D texture, int columns, int rows, int frameX = 0, int frameY = 0)
        {
            int frameWidth = texture.Width / columns;
            int frameHeight = texture.Height / rows;

            Texture2D frameTexture = new Texture2D(Main.graphics.GraphicsDevice, frameWidth, frameHeight);
            Color[] fullPixels = new Color[texture.Width * texture.Height];
            texture.GetData(fullPixels);

            Color[] framePixels = new Color[frameWidth * frameHeight];
            for (int y = 0; y < frameHeight; y++)
            {
                for (int x = 0; x < frameWidth; x++)
                {
                    int sourceX = x + frameX * frameWidth;
                    int sourceY = y + frameY * frameHeight;
                    framePixels[y * frameWidth + x] = fullPixels[sourceY * texture.Width + sourceX];
                }
            }

            frameTexture.SetData(framePixels);
            return ConvertTextureToBase64(frameTexture);
        }
    }
}
