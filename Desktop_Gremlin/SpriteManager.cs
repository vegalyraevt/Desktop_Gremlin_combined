using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Desktop_Gremlin
{
    public static class SpriteManager
    {
        public static int PlayAnimation(string sheetName, string actionType, int currentFrame, int frameCount, System.Windows.Controls.Image targetImage, bool PlayOnce = false)
        {
            BitmapImage sheet = SpriteManager.Get(sheetName, actionType);
            if (sheet == null)
            {
                return currentFrame;
            }
            
            // Get the appropriate dimensions - use dance-specific or default
            // Prevent divide by zero with early safety defaults
            int column = Settings.SpriteColumn > 0 ? Settings.SpriteColumn : 10;
            int frameWidth = Settings.FrameWidth > 0 ? Settings.FrameWidth : 300;
            int frameHeight = Settings.FrameHeight > 0 ? Settings.FrameHeight : 300;
            int originalFrameWidth = frameWidth;   // Store original for padding calculation
            int originalFrameHeight = frameHeight; // Store original for padding calculation
            
            // Handle dance animation with different sprite sheet format
            int danceFrameWidth = 0;
            int danceFrameHeight = 0;
            if (sheetName.Equals("Dance", StringComparison.OrdinalIgnoreCase))
            {
                // Dance sprites from JukeBox use 5 columns with 300px width
                // But height varies per character (e.g., Oguri is 340px tall)
                // Dynamically calculate frame height from sprite sheet dimensions
                column = 5; // JukeBox dance sprites always use 5 columns
                danceFrameWidth = sheet.PixelWidth / column; // Should be 300
                
                // Calculate number of rows needed for frameCount
                int numRows = (frameCount + column - 1) / column; // Ceiling division
                if (numRows <= 0) numRows = 1;
                
                // Calculate actual frame height from image height and row count
                danceFrameHeight = sheet.PixelHeight / numRows;
                
                frameWidth = danceFrameWidth;
            }
            
            // Final safety check - ensure no division by zero possible
            if (column <= 0) column = 1;
            if (frameWidth <= 0) frameWidth = 1;
            if (frameHeight <= 0) frameHeight = 1;
            
            // Use dance frame height for cropping if in dance mode
            int cropHeight = danceFrameHeight > 0 ? danceFrameHeight : frameHeight;
            
            int x = (currentFrame % column) * frameWidth;
            int y = (currentFrame / column) * cropHeight;
            if (x + frameWidth > sheet.PixelWidth || y + cropHeight > sheet.PixelHeight)
            {
                return currentFrame;
            }
            
            // For dance animation, pad the frame to match normal sprite dimensions
            if (danceFrameHeight > 0 && (danceFrameHeight < originalFrameHeight || danceFrameWidth < originalFrameWidth))
            {
                // Create padded image with transparent padding (top for height, centered for width)
                int paddingTop = originalFrameHeight - danceFrameHeight;
                int paddingLeft = (originalFrameWidth - danceFrameWidth) / 2; // Center horizontally
                var croppedFrame = new CroppedBitmap(sheet, new Int32Rect(x, y, danceFrameWidth, danceFrameHeight));
                
                // Create a DrawingVisual to compose the padded image
                var visual = new System.Windows.Media.DrawingVisual();
                using (var context = visual.RenderOpen())
                {
                    // Draw the dance frame centered horizontally, at bottom vertically
                    context.DrawImage(croppedFrame, new Rect(paddingLeft, paddingTop, danceFrameWidth, danceFrameHeight));
                }
                
                var renderBitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
                    originalFrameWidth, originalFrameHeight, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                renderBitmap.Render(visual);
                targetImage.Source = renderBitmap;
            }
            else
            {
                targetImage.Source = new CroppedBitmap(sheet, new Int32Rect(x, y, frameWidth, frameHeight));
            }
            
            // Prevent divide by zero on frameCount
            if (frameCount <= 0)
            {
                return 0; // Reset to first frame instead of crashing
            }              
            return (currentFrame + 1) % frameCount;
        }
        public static BitmapImage Get(string animationName, string actionType)
        {
            BitmapImage sheet = null;
            
            // Check if character supports this animation type
            if (!IsAnimationSupportedByCharacter(animationName, Settings.StartingChar))
            {
                // Silently return null for unsupported animations instead of showing error
                return null;
            }
            
            // Get character-specific animation name
            string characterSpecificAnimationName = CharacterFeatureManager.GetCharacterSpecificAnimationName(
                Settings.StartingChar, animationName);
            
            string fileName = GetFileName(characterSpecificAnimationName);
            if (fileName == null)
            {
                // Try with original name as fallback
                fileName = GetFileName(animationName);
                if (fileName == null)
                {
                    return null; // Silently fail for missing animations
                }
            }
            
            sheet = LoadSprite(Settings.StartingChar, fileName, actionType);
            return sheet;
        }
        
        /// <summary>
        /// Checks if a character supports a specific animation
        /// </summary>
        private static bool IsAnimationSupportedByCharacter(string animationName, string characterName)
        {
            switch (animationName.ToLower())
            {
                case "fireleft":
                case "fireright":
                    return CharacterFeatureManager.CharacterHasFeature(characterName, "fire");
                case "reload":
                    return CharacterFeatureManager.CharacterHasFeature(characterName, "ammo");
                case "emote1":
                case "emote2":
                case "emote3":
                case "emote4":
                    return CharacterFeatureManager.CharacterHasFeature(characterName, "emote");
                case "walkleft":
                case "walkright":
                case "walkup":
                case "walkdown":
                    return CharacterFeatureManager.CharacterHasFeature(characterName, "walk");
                case "jumpscare":
                    return CharacterFeatureManager.CharacterHasFeature(characterName, "jumpscare");
                case "sleep":
                case "sleeping":
                    return CharacterFeatureManager.CharacterHasFeature(characterName, "sleep");
                case "dance":
                    return CharacterFeatureManager.CharacterHasFeature(characterName, "dance");
                default:
                    return true; // Basic animations like idle, intro, outro, run are supported by all
            }
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
                case "dance":
                    return "dance.png";
                case "sleeping":
                    return "sleep.png";
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
            
            // Also try alternate file names for old format characters
            if (!File.Exists(path))
            {
                string altFileName = GetOldFormatFileName(fileName, action);
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
        /// Maps new format file names to old format equivalents
        /// </summary>
        private static string GetOldFormatFileName(string newFileName, string action)
        {
            // Old format uses different naming conventions
            switch (newFileName.ToLower())
            {
                // Run directions - old format uses different names
                case "runleft.png":
                    return "left.png";
                case "runright.png":
                    return "right.png";
                case "runup.png":
                    return "backward.png";
                case "rundown.png":
                    return "forward.png";
                    
                // Walk - old format uses walkL/walkR instead of walkLeft/walkRight
                case "walkleft.png":
                    return "walkL.png";
                case "walkright.png":
                    return "walkR.png";
                    
                // Idle in walk context
                case "runidle.png":
                    return "wIdle.png";
                    
                default:
                    return null;
            }
        }
       
    }
}
