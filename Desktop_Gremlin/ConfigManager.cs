using Desktop_Gremlin;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;

public static class ConfigManager
{
    public static void LoadMasterConfig()
    {
        string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");
        if (!File.Exists(path))
        {
            Gremlin.ErrorClose("Cannot find the Main config.txt", "Missing config.txt", true);
            return;
        }

        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line) || !line.Contains("="))
            {
                continue;
            }

            var parts = line.Split('=');
            if (parts.Length != 2)
            {
                continue;
            }

            string key = parts[0].Trim();
            string value = parts[1].Trim();

            switch (key.ToUpper())
            {
                case "START_CHAR":
                    {
                        Settings.StartingChar = value;
                        break;
                    }
                case "COMPANION_CHAR":
                    {
                        Settings.CompanionChar = value;
                        break;
                    }
                case "SPRITE_FRAMERATE":
                    {
                        if (int.TryParse(value, out int intValue))
                        {
                            Settings.FrameRate = intValue;
                        }
                        break;
                    }
                case "FOLLOW_RADIUS":
                    {
                        if (double.TryParse(value, out double intValue))
                        {
                            Settings.FollowRadius = intValue;
                        }
                        break;
                    }
                case "MAX_INTERVAL":
                    {
                        if (int.TryParse(value, out int intValue))
                        {
                            Settings.RandomMaxInterval = intValue;
                        }
                    }
                    break;
                case "MIN_INTERVAL":
                    {
                        if (int.TryParse(value, out int intValue))
                        {
                            Settings.RandomMinInterval = intValue;
                        }
                    }
                    break;
                case "RANDOM_MOVE_DISTANCE":
                    {
                        if (int.TryParse(value, out int intValue))
                        {
                            Settings.MoveDistance = intValue;
                        }
                    }
                    break;
                case "ALLOW_RANDOM_ACTIONS":
                    {
                        if (bool.TryParse(value, out bool Value))
                        {
                            Settings.AllowRandomness = Value;
                        }
                    }
                    break;
                case "ALLOW_GRAVITY":
                    {
                        if (bool.TryParse(value, out bool Value))
                        {
                            Settings.AllowGravity = Value;
                        }
                    }
                    break;
                case "SLEEP_TIME":
                    {
                        if (int.TryParse(value, out int Value))
                        {
                            Settings.SleepTime = Value;
                        }
                    }
                    break;
                case "ALLOW_FOOTSTEP_SOUNDS":
                    {
                        if (bool.TryParse(value, out bool Value))
                        {
                            Settings.FootStepSounds = Value;
                        }
                    }
                    break;
                case "AMMO":
                    {
                        if (int.TryParse(value, out int Value))
                        {
                            Settings.Ammo = Value;
                        }
                    }
                    break;

                case "ALLOW_COLOR_HOTSPOT":
                    {
                        if (bool.TryParse(value, out bool Value))
                        {
                            Settings.AllowColoredHotSpot = Value;
                        }
                    }
                    break;
                case "SHOW_TASKBAR":
                    {
                        if (bool.TryParse(value, out bool Value))
                        {
                            Settings.ShowTaskBar = Value;
                        }
                    }
                    break;
                case "SPRITE_SCALE":
                    {
                        if (double.TryParse(value, out double Value))
                        {
                            Settings.SpriteSize = Value;
                        }
                    }
                    break;
                case "FORCE_FAKE_TRANSPARENT":
                    {
                        if (bool.TryParse(value, out bool Value))
                        {
                            Settings.FakeTransparent = Value;
                        }
                    }
                    break;
                case "ALLOW_ERROR_MESSAGES":
                    {
                        if (bool.TryParse(value, out bool Value))
                        {
                            Settings.AllowErrorMessages = Value;
                        }
                    }
                    break;
                case "MAX_ACCELERATION":
                    {
                        if (int.TryParse(value, out int Value))
                        {
                            Settings.MaxItemAcceleration = Value;
                        }
                    }
                    break;
                case "FOLLOW_ACCELERATION":
                    {
                        if (double.TryParse(value, out double Value))
                        {
                            Settings.ItemAcceleration = Value;
                        }
                    }
                    break;
                case "CURRENT_ACCELERATION":
                    {
                        if (double.TryParse(value, out double Value))
                        {
                            Settings.ItemAcceleration = Value;
                        }
                    }
                    break;
                case "MAX_EATING_SIZE":
                    {
                        if (int.TryParse(value, out int Value))
                        {
                            Settings.FoodItemGetSize = Value;
                        }
                    }
                    break;
                case "ITEM_WIDTH":
                    {
                        if (int.TryParse(value, out int Value))
                        {
                            Settings.ItemWidth = Value;
                        }
                    }
                    break;
                case "ITEM_HEIGHT":
                    {
                        if (int.TryParse(value, out int Value))
                        {
                            Settings.ItemHeight = Value;
                        }
                    }
                    break;
                case "COMPANIONS_SCALE":
                    {
                        if (double.TryParse(value, out double Value))
                        {
                            Settings.CompanionScale = Value;
                        }
                    }
                    break;
                case "ENABLE_MIN_RESIZE":
                    {
                        if (bool.TryParse(value, out bool Value))
                        {
                            Settings.EnableMinSize = Value;
                        }
                    }
                    break;
                case "FORCE_CENTER":
                    {
                        if (bool.TryParse(value, out bool Value))
                        {
                            Settings.ForceCenter = Value;
                        }
                    }
                    break;
                case "ENABLE_MANUAL_RESIZE":
                    {
                        if (bool.TryParse(value, out bool Value))
                        {
                            Settings.ManualReize = Value;
                        }
                    }
                    break;
                case "VOLUME_LEVEL":
                    {
                        if (double.TryParse(value, out double Value))
                        {
                            Settings.VolumeLevel = Value;
                        }
                    }
                    break;
                case "SELECTED_ICON":
                    {
                        Settings.SelectedIcon = value;
                    }
                    break;
            }

        }
    }
    
    public static FrameCounts LoadConfigChar(string character)
    {
        var result = new FrameCounts();
        string path = System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "SpriteSheet", "Gremlins", character, "config.txt");

        if (!File.Exists(path))
        {
            Gremlin.ErrorClose("Cannot find the SpriteSheet config.txt", "Missing config.txt", true);
            return result;
        }

        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line) || !line.Contains("="))
            {
                continue;
            }
                

            var parts = line.Split('=');
            if (parts.Length != 2)
            {
                continue;
            }
            
            string key = parts[0].Trim();
            string value = parts[1].Trim();

            if (!int.TryParse(value, out int intValue))
            {
                continue;
            }
                
            switch (key.ToUpper())
            {
                case "INTRO": result.Intro = intValue; break;
                case "IDLE": result.Idle = intValue; break;
                case "IDLE2": result.Idle2 = intValue; break;
                case "RUNUP": 
                case "UP":
                    result.Up = intValue; 
                    break;
                case "RUNDOWN": 
                case "DOWN":
                    result.Down = intValue; 
                    break;
                case "RUNLEFT": 
                case "LEFT":
                    result.Left = intValue; 
                    break;
                case "RUNRIGHT": 
                case "RIGHT":
                    result.Right = intValue; 
                    break;
                case "UPLEFT": result.UpLeft = intValue; break;
                case "UPRIGHT": result.UpRight = intValue; break;
                case "DOWNLEFT": result.DownLeft = intValue; break;
                case "DOWNRIGHT": result.DownRight = intValue; break;
                case "OUTRO": result.Outro = intValue; break;
                case "GRAB": result.Grab = intValue; break;
                case "RUNIDLE": 
                case "WALK_IDLE":  // Legacy name used by some characters like Agnes
                    result.RunIdle = intValue; 
                    break;
                case "CLICK": result.Click = intValue; break;
                case "HOVER": result.Hover = intValue; break;
                case "SLEEP": result.Sleep = intValue; break;
                case "FIREL": 
                case "LEFTFIRE":
                case "FIRELEFT":
                    // Only set if character supports fire mechanics
                    if (CharacterFeatureManager.CharacterHasFeature(character, "fire"))
                    {
                        result.LeftFire = intValue; 
                    }
                    break;
                case "FIRER": 
                case "RIGHTFIRE":
                case "FIRERIGHT":
                    // Only set if character supports fire mechanics
                    if (CharacterFeatureManager.CharacterHasFeature(character, "fire"))
                    {
                        result.RightFire = intValue; 
                    }
                    break;
                case "RELOAD": 
                    // Only set if character supports ammo system
                    if (CharacterFeatureManager.CharacterHasFeature(character, "ammo"))
                    {
                        result.Reload = intValue; 
                    }
                    break;
                case "PAT": result.Pat = intValue; break;
                case "WALKLEFT": 
                case "WALK_L":
                    result.WalkL = intValue; 
                    break;
                case "WALKRIGHT": 
                case "WALK_R":
                    result.WalkR = intValue; 
                    break;
                case "WALKUP": 
                case "WALK_U":
                    result.WalkUp = intValue; 
                    break;
                case "WALKDOWN": 
                case "WALK_D":
                    result.WalkDown = intValue; 
                    break;
                case "EMOTE1": result.Emote1 = intValue; break;
                case "EMOTE2": result.Emote2 = intValue; break;
                case "EMOTE3": result.Emote3 = intValue; break;
                case "EMOTE4": result.Emote4 = intValue; break;
                case "DANCE": result.Dance = intValue; break;
                case "JUMPSCARE": result.JumpScare = intValue; break;
                case "POOF": result.Poof = intValue; break;
                case "WIDTH": Settings.FrameWidth = intValue; break;
                case "HEIGHT": Settings.FrameHeight = intValue; break;
                case "COLUMN": Settings.SpriteColumn = intValue; break;
                case "WIDTHJS": Settings.FrameWidthJs = intValue; break;
                case "HEIGHTJS": Settings.FrameHeightJs = intValue; break;
            }
        }
        return result;
    }

    /// <summary>
    /// Loads character config for companion - stores dimensions in companion-specific settings
    /// </summary>
    public static FrameCounts LoadConfigCharCompanion(string character)
    {
        var result = new FrameCounts();
        string path = System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "SpriteSheet", "Gremlins", character, "config.txt");

        if (!File.Exists(path))
        {
            // Companion config missing - not fatal, just return empty
            return result;
        }

        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line) || !line.Contains("="))
            {
                continue;
            }

            var parts = line.Split('=');
            if (parts.Length != 2)
            {
                continue;
            }
            
            string key = parts[0].Trim();
            string value = parts[1].Trim();

            if (!int.TryParse(value, out int intValue))
            {
                continue;
            }
                
            switch (key.ToUpper())
            {
                case "INTRO": result.Intro = intValue; break;
                case "IDLE": result.Idle = intValue; break;
                case "IDLE2": result.Idle2 = intValue; break;
                case "RUNUP": 
                case "UP":
                    result.Up = intValue; 
                    break;
                case "RUNDOWN": 
                case "DOWN":
                    result.Down = intValue; 
                    break;
                case "RUNLEFT": 
                case "LEFT":
                    result.Left = intValue; 
                    break;
                case "RUNRIGHT": 
                case "RIGHT":
                    result.Right = intValue; 
                    break;
                case "UPLEFT": result.UpLeft = intValue; break;
                case "UPRIGHT": result.UpRight = intValue; break;
                case "DOWNLEFT": result.DownLeft = intValue; break;
                case "DOWNRIGHT": result.DownRight = intValue; break;
                case "OUTRO": result.Outro = intValue; break;
                case "GRAB": result.Grab = intValue; break;
                case "RUNIDLE":
                case "WALK_IDLE":  // Legacy name used by some characters like Agnes
                    result.RunIdle = intValue; 
                    break;
                case "CLICK": result.Click = intValue; break;
                case "HOVER": result.Hover = intValue; break;
                case "SLEEP": result.Sleep = intValue; break;
                case "PAT": result.Pat = intValue; break;
                case "POOF": result.Poof = intValue; break;
                // Store in COMPANION settings, not main settings
                case "WIDTH": Settings.CompFrameWidth = intValue; break;
                case "HEIGHT": Settings.CompFrameHeight = intValue; break;
                case "COLUMN": Settings.CompSpriteColumn = intValue; break;
            }
        }
        return result;
    }


    public static void ApplyXamlSettings(Gremlin window)
    {
        if (window == null)
        {
            return;
        }

        bool useColors = Settings.AllowColoredHotSpot;
        bool showTaskBar = Settings.ShowTaskBar;
        double scale = Settings.SpriteSize;

        ApplySettings(window, Settings.AllowColoredHotSpot, Settings.ShowTaskBar,Settings.SpriteSize,Settings.FakeTransparent,
            Settings.ManualReize,Settings.ForceCenter,Settings.EnableMinSize
            );
    }
    private static void ApplySettings(Gremlin window, bool useColors, bool showTaskBar, double scale, 
        bool useFakeTransparent, bool useManualReize, bool forCenter, bool enableMinResize)
    {
        Border LeftHotspot = window.LeftHotspot;
        Border LeftDownHotspot = window.LeftDownHotspot;
        Border RightHotspot = window.RightHotspot;
        Border RightDownHotspot = window.RightDownHotspot;
        Border TopHotspot = window.TopHotspot;
        System.Windows.Controls.Image SpriteImage = window.SpriteImage;

        if (useColors)
        {
            LeftHotspot.Background = new SolidColorBrush(Colors.Red);
            LeftDownHotspot.Background = new SolidColorBrush(Colors.Yellow);
            RightHotspot.Background = new SolidColorBrush(Colors.Blue);
            RightDownHotspot.Background = new SolidColorBrush(Colors.Orange);
            TopHotspot.Background = new SolidColorBrush(Colors.Purple);
        }
        else
        {
            var noColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#01000000"));
            LeftHotspot.Background = noColor;
            LeftDownHotspot.Background = noColor;
            RightHotspot.Background = noColor;
            RightDownHotspot.Background = noColor;
            TopHotspot.Background = noColor;
        }

        window.ShowInTaskbar = showTaskBar;

        if (useFakeTransparent)
        {
            window.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom("#01000000"));
        }
        if (useManualReize)
        {
            window.SizeToContent = SizeToContent.Manual;    
        }
        if(forCenter)
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        double baseLeftW = LeftHotspot.Width, baseLeftH = LeftHotspot.Height;
        double baseLeftDownW = LeftDownHotspot.Width, baseLeftDownH = LeftDownHotspot.Height;
        double baseRightW = RightHotspot.Width, baseRightH = RightHotspot.Height;
        double baseRightDownW = RightDownHotspot.Width, baseRightDownH = RightDownHotspot.Height;
        double baseTopW = TopHotspot.Width, baseTopH = TopHotspot.Height;

        double originalWidth = SpriteImage.Width;
        double originalHeight = SpriteImage.Height;

        double newWidth = originalWidth * scale;
        double newHeight = originalHeight * scale;
        window.Width = window.Height * scale;
        window.Height = window.Height * scale;  

        SpriteImage.Width = newWidth;
        SpriteImage.Height = newHeight;
        if(enableMinResize)
        {
            window.MinWidth = window.Width;
            window.MinHeight = window.Height;
        }
       double leftHotspotOffsetX = LeftHotspot.Margin.Left - SpriteImage.Margin.Left;
        double leftHotspotOffsetY = LeftHotspot.Margin.Top - SpriteImage.Margin.Top;

        double leftDownOffsetX = LeftDownHotspot.Margin.Left - SpriteImage.Margin.Left;
        double leftDownOffsetY = LeftDownHotspot.Margin.Top - SpriteImage.Margin.Top;

        double rightOffsetX = RightHotspot.Margin.Left - SpriteImage.Margin.Left;
        double rightOffsetY = RightHotspot.Margin.Top - SpriteImage.Margin.Top;

        double rightDownOffsetX = RightDownHotspot.Margin.Left - SpriteImage.Margin.Left;
        double rightDownOffsetY = RightDownHotspot.Margin.Top - SpriteImage.Margin.Top;

        double topOffsetX = TopHotspot.Margin.Left - SpriteImage.Margin.Left;
        double topOffsetY = TopHotspot.Margin.Top - SpriteImage.Margin.Top;

        double centerX = (window.Width - newWidth) / 2;
        double centerY = (window.Height - newHeight) / 2;

        SpriteImage.Margin = new Thickness(centerX, centerY, 0, 0);

        double scaleX = newWidth / originalWidth;
        double scaleY = newHeight / originalHeight;
        ScaleHotspot(LeftHotspot, leftHotspotOffsetX, leftHotspotOffsetY, scaleX, scaleY, centerX, centerY, baseLeftW, baseLeftH);
        ScaleHotspot(LeftDownHotspot, leftDownOffsetX, leftDownOffsetY, scaleX, scaleY, centerX, centerY, baseLeftDownW, baseLeftDownH);
        ScaleHotspot(RightHotspot, rightOffsetX, rightOffsetY, scaleX, scaleY, centerX, centerY, baseRightW, baseRightH);
        ScaleHotspot(RightDownHotspot, rightDownOffsetX, rightDownOffsetY, scaleX, scaleY, centerX, centerY, baseRightDownW, baseRightDownH);
        ScaleHotspot(TopHotspot, topOffsetX, topOffsetY, scaleX, scaleY, centerX, centerY, baseTopW, baseTopH);
    }

    private static void ScaleHotspot(Border hotspot, double offsetX, double offsetY, double scaleX,
    double scaleY, double centerX, double centerY, double baseWidth, double baseHeight)
    {
        hotspot.Width = baseWidth * scaleX;
        hotspot.Height = baseHeight * scaleY;
        hotspot.Margin = new Thickness(centerX + offsetX * scaleX, centerY + offsetY * scaleY, 0, 0);
        
    }
    public class AppConfig
    {
        private readonly Window _window;
        private NotifyIcon _trayIcon;
        public AnimationStates _states;    
        public AppConfig(Window window, AnimationStates states)
        {
            _window = window;
            _states = states;
            SetupTrayIcon();
        }   
        public void SetupTrayIcon()
        {
            _trayIcon = new NotifyIcon();
            
            // Load the selected icon
            LoadSelectedIcon();

            _trayIcon.Visible = true;
            _trayIcon.Text = "Desktop Gremlin";

            var menu = new ContextMenuStrip();
            menu.Items.Add("Switch Character", null, (s, e) => ShowCharacterSelector());
            menu.Items.Add("Dance! 💃", null, (s, e) => TriggerDance());
            menu.Items.Add("Toggle Combat Mode 🔫", null, (s, e) => ToggleCombatMode());
            
            // Add icon selection submenu
            var iconMenu = new ToolStripMenuItem("Change Icon 🎨");
            string iconsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Icons");
            if (Directory.Exists(iconsPath))
            {
                foreach (var iconFile in Directory.GetFiles(iconsPath, "*.ico"))
                {
                    string iconName = Path.GetFileNameWithoutExtension(iconFile);
                    var menuItem = new ToolStripMenuItem(iconName);
                    menuItem.Checked = (iconName == Settings.SelectedIcon);
                    menuItem.Click += (s, e) => ChangeIcon(iconName);
                    iconMenu.DropDownItems.Add(menuItem);
                }
            }
            menu.Items.Add(iconMenu);
            
            menu.Items.Add("-"); // Separator
            menu.Items.Add("Debug View 🔧", null, (s, e) => DebugOverlay.Toggle());
            menu.Items.Add("Settings ⚙️", null, (s, e) => SettingsWindow.ShowSettings());
            menu.Items.Add("-"); // Separator
            menu.Items.Add("Stylish Close", null, (s, e) => CloseApp());
            menu.Items.Add("Force Close", null, (s, e) => ForceClose());
            menu.Items.Add("Restart", null, (s, e) => RestartApp());
            _trayIcon.ContextMenuStrip = menu;
        }
        
        private void LoadSelectedIcon()
        {
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Icons", Settings.SelectedIcon + ".ico");
            
            if (File.Exists(iconPath))
            {
                _trayIcon.Icon = new Icon(iconPath);
            }
            else if (File.Exists("SpriteSheet/System/ico.ico"))
            {
                _trayIcon.Icon = new Icon("SpriteSheet/System/ico.ico");
            }
            else
            {
                _trayIcon.Icon = SystemIcons.Application;
            }
        }
        
        private void ChangeIcon(string iconName)
        {
            Settings.SelectedIcon = iconName;
            
            // Update the tray icon
            LoadSelectedIcon();
            
            // Save to config file
            SaveIconSetting(iconName);
            
            // Update menu checkmarks
            var menu = _trayIcon.ContextMenuStrip;
            foreach (ToolStripItem item in menu.Items)
            {
                if (item is ToolStripMenuItem iconMenu && iconMenu.Text == "Change Icon 🎨")
                {
                    foreach (ToolStripMenuItem subItem in iconMenu.DropDownItems)
                    {
                        subItem.Checked = (subItem.Text == iconName);
                    }
                    break;
                }
            }
        }
        
        private void SaveIconSetting(string iconName)
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");
                if (!File.Exists(configPath)) return;
                
                var lines = File.ReadAllLines(configPath).ToList();
                bool found = false;
                
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].TrimStart().StartsWith("SELECTED_ICON", StringComparison.OrdinalIgnoreCase))
                    {
                        lines[i] = $"SELECTED_ICON = {iconName}";
                        found = true;
                        break;
                    }
                }
                
                if (!found)
                {
                    lines.Add($"\n//Icon Setting\nSELECTED_ICON = {iconName}");
                }
                
                File.WriteAllLines(configPath, lines);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Failed to save icon setting: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ShowCharacterSelector()
        {
            try
            {
                var selector = new CharacterSelector();
                var result = selector.ShowDialog();
                
                if (selector.CharacterSelected && !string.IsNullOrEmpty(selector.SelectedCharacter))
                {
                    SwitchCharacter(selector.SelectedCharacter);
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Failed to open character selector: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void TriggerDance()
        {
            try
            {
                // Check if current character supports dancing
                if (!CharacterFeatureManager.CharacterHasFeature(Settings.StartingChar, "dance"))
                {
                    System.Windows.Forms.MessageBox.Show(
                        $"{Settings.StartingChar} doesn't know how to dance!\n\nCharacters that can dance:\n• RiceShower\n• Agnes Tachyon\n• Oguri", 
                        "No Dancing 💃", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Information);
                    return;
                }
                
                // Trigger dance animation in the main window
                if (_window is Gremlin gremlinWindow)
                {
                    gremlinWindow.TriggerDance();
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Failed to trigger dance: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void ToggleCombatMode()
        {
            try
            {
                // Only works for Exusiai characters
                if (Settings.StartingChar != "Exusiai" && Settings.StartingChar != "Exusiai_Gun")
                {
                    System.Windows.Forms.MessageBox.Show("Combat mode is only available for Exusiai!", "Combat Mode 🔫", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                // Toggle combat mode in the main window
                if (_window is Gremlin gremlinWindow)
                {
                    gremlinWindow.ToggleCombatMode();
                    UpdateTrayText();
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Failed to toggle combat mode: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void SwitchCharacter(string newCharacter)
        {
            try
            {
                // Switch to new character
                if (CharacterManager.SwitchCharacter(newCharacter))
                {
                    // Update tray text
                    UpdateTrayText();
                    
                    // Notify the main window to reload character
                    if (_window is Gremlin gremlinWindow)
                    {
                        gremlinWindow.ReloadCharacter();
                    }
                    
                    // Update config file to persist selection
                    UpdateConfigFile(newCharacter);
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show($"Failed to switch to character: {newCharacter}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error switching character: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void UpdateTrayText()
        {
            var currentChar = CharacterManager.GetCurrentCharacter();
            if (!string.IsNullOrEmpty(currentChar))
            {
                _trayIcon.Text = $"Desktop Gremlin - {currentChar}";
            }
            else
            {
                _trayIcon.Text = "Desktop Gremlin";
            }
        }
        
        private void UpdateConfigFile(string newCharacter)
        {
            try
            {
                string configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");
                if (File.Exists(configPath))
                {
                    var lines = File.ReadAllLines(configPath);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].StartsWith("START_CHAR"))
                        {
                            lines[i] = $"START_CHAR = {newCharacter}";
                            break;
                        }
                    }
                    File.WriteAllLines(configPath, lines);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to update config file: {ex.Message}");
            }
        }

        public void CloseApp()
        {
           _states.PlayOutro();  
            MediaManager.PlaySound("outro.wav", Settings.StartingChar); 
        }
        private void ForceClose()
        {
            System.Windows.Application.Current.Shutdown();
        }
        private void RestartApp()
        {
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            Process.Start(exePath);
            System.Windows.Application.Current.Shutdown();
        }
    }

}

