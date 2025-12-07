using Desktop_Gremlin;
using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.IO;

namespace Mambo
{
    internal class SpriteManagerComp
    {
        public static int PlayAnimation(string sheetName, string actionType, int currentFrame, int frameCount, System.Windows.Controls.Image targetImage, bool PlayOnce = false)
        {
            BitmapImage sheet = SpriteManagerComp.Get(sheetName, actionType);
            if (sheet == null)
            {
                return currentFrame;
            }
            
            // Use companion-specific settings with safety defaults
            int column = Settings.CompSpriteColumn > 0 ? Settings.CompSpriteColumn : 10;
            int frameWidth = Settings.CompFrameWidth > 0 ? Settings.CompFrameWidth : 300;
            int frameHeight = Settings.CompFrameHeight > 0 ? Settings.CompFrameHeight : 300;
            
            int x = (currentFrame % column) * frameWidth;
            int y = (currentFrame / column) * frameHeight;
            if (x + frameWidth > sheet.PixelWidth || y + frameHeight > sheet.PixelHeight)
            {
                return currentFrame;
            }
            targetImage.Source = new CroppedBitmap(sheet, new Int32Rect(x, y, frameWidth, frameHeight));
            if (frameCount <= 0)
            {
                return 0; // Return safely instead of crashing
            }
            return (currentFrame + 1) % frameCount;
        }
        public static int PlayEffect(string sheetName, string actionType, int currentFrame, int frameCount, System.Windows.Controls.Image targetImage, bool PlayOnce = false)
        {
            BitmapImage sheet = SpriteManagerComp.Get(sheetName, actionType);
            if (sheet == null)
            {
                return currentFrame;
            }
            
            // Use companion-specific settings with safety defaults
            int column = Settings.CompSpriteColumn > 0 ? Settings.CompSpriteColumn : 10;
            int frameWidth = Settings.CompFrameWidth > 0 ? Settings.CompFrameWidth : 300;
            int frameHeight = Settings.CompFrameHeight > 0 ? Settings.CompFrameHeight : 300;
            
            int x = (currentFrame % column) * frameWidth;
            int y = (currentFrame / column) * frameHeight;
            if (x + frameWidth > sheet.PixelWidth || y + frameHeight > sheet.PixelHeight)
            {
                return currentFrame;
            }
            targetImage.Source = new CroppedBitmap(sheet, new Int32Rect(x, y, frameWidth, frameHeight));
            if (frameCount <= 0)
            {
                return 0; // Return safely instead of crashing
            }
            return (currentFrame + 1) % frameCount;
        }
        public static BitmapImage Get(string animationName, string actionType)
        {
            BitmapImage sheet = null;
            string fileName = GetFileName(animationName);
            if (fileName == null)
            {
                Gremlin.ErrorClose("Error Animation: " + animationName + " is missing", "Animation Missing", false);
                return null;
            }
            sheet = LoadSprite(Settings.CompanionChar, fileName, actionType);
            return sheet;
        }
        private static string GetFileName(string animationName)
        {
            switch (animationName.ToLower())
            {
                case "idle":
                    return "idle.png";
                case "idle2":
                    return "idle2.png";
                case "intro":
                    return "intro.png";
                case "runleft":
                    return "runLeft.png";
                case "runright":
                    return "runRight.png";
                case "runup":
                    return "runUp.png";
                case "rundown":
                    return "runDown.png";
                case "outro":
                    return "outro.png";
                case "grab":
                    return "grab.png";
                case "runidle":
                    return "runIdle.png";
                case "click":
                    return "click.png";
                case "hover":
                    return "hover.png";
                case "sleep":
                    return "sleep.png";
                case "fireleft":
                    return "fireLeft.png";
                case "fireright":
                    return "fireRight.png";
                case "reload":
                    return "reload.png";
                case "pat":
                    return "pat.png";
                case "upleft":
                    return "upLeft.png";
                case "upright":
                    return "upRight.png";
                case "downleft":
                    return "downLeft.png";
                case "downright":
                    return "downRight.png";
                case "walkleft":
                    return "walkLeft.png";
                case "walkright":
                    return "walkRight.png";
                case "walkdown":
                    return "walkDown.png";
                case "walkup":
                    return "walkUp.png";
                case "emote1":
                    return "emote1.png";
                case "emote2":
                    return "emote2.png";
                case "emote3":
                    return "emote3.png";
                case "emote4":
                    return "emote4.png";
                case "jumpscare":
                    return "jumpScare.png";
                case "poof":
                    return "poof.png";
                default:
                    return null;
            }
        }
        private static BitmapImage LoadSprite(string filefolder, string fileName, string action, string rootFolder = "Gremlins")
        {
            string basePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "SpriteSheet", rootFolder, filefolder);
            
            // Try new format first: Character/Action/file.png
            string path = System.IO.Path.Combine(basePath, action, fileName);
            
            // If not found, try old format: Character/file.png (flat structure)
            if (!File.Exists(path))
            {
                path = System.IO.Path.Combine(basePath, fileName);
            }
            
            // Also try alternate file names for legacy format characters
            if (!File.Exists(path))
            {
                string altFileName = GetLegacyFileName(fileName, action);
                if (altFileName != null)
                {
                    // Try in action subfolder first
                    path = System.IO.Path.Combine(basePath, action, altFileName);
                    if (!File.Exists(path))
                    {
                        // Then try flat structure
                        path = System.IO.Path.Combine(basePath, altFileName);
                    }
                }
            }
            
            if (!File.Exists(path))
                return null;
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(path);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Maps new format file names to legacy format equivalents
        /// </summary>
        private static string GetLegacyFileName(string newFileName, string action)
        {
            switch (newFileName.ToLower())
            {
                // Run directions - legacy uses left/right instead of runLeft/runRight
                case "runleft.png":
                    return "left.png";
                case "runright.png":
                    return "right.png";
                case "runup.png":
                    return "backward.png";
                case "rundown.png":
                    return "forward.png";
                    
                // Walk idle - legacy uses wIdle or walkIdle
                case "runidle.png":
                    return "wIdle.png";
                    
                // Walk directions
                case "walkleft.png":
                    return "walkL.png";
                case "walkright.png":
                    return "walkR.png";
                case "walkup.png":
                    return "walkU.png";
                case "walkdown.png":
                    return "walkD.png";
                    
                default:
                    return null;
            }
        }
    }
}
