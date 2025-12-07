using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace Desktop_Gremlin
{
    public class CharacterInfo
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public int SpriteColumn { get; set; }
        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
        public FrameCounts FrameCounts { get; set; } = new FrameCounts();
        public bool IsAvailable { get; set; } = true;
        
        public CharacterInfo()
        {
        }
        
        public CharacterInfo(string name)
        {
            Name = name;
            DisplayName = name;
        }
    }

    public static class CharacterManager
    {
        private static Dictionary<string, CharacterInfo> _availableCharacters = new Dictionary<string, CharacterInfo>();
        private static string _currentCharacter = "";
        
        /// <summary>
        /// Scans the Gremlins folder and loads all available characters
        /// </summary>
        public static void Initialize()
        {
            _availableCharacters.Clear();
            
            string gremlinsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SpriteSheet", "Gremlins");
            
            if (!Directory.Exists(gremlinsPath))
            {
                Gremlin.ErrorClose("Cannot find SpriteSheet/Gremlins folder", "Missing Characters", true);
                return;
            }
            
            // Initialize character features first
            CharacterFeatureManager.InitializeCharacterFeatures();
            
            // Scan for character folders
            var characterDirectories = Directory.GetDirectories(gremlinsPath);
            
            foreach (var charDir in characterDirectories)
            {
                string charName = Path.GetFileName(charDir);
                string configPath = Path.Combine(charDir, "config.txt");
                
                if (File.Exists(configPath))
                {
                    try
                    {
                        var characterInfo = LoadCharacterConfig(charName, configPath);
                        _availableCharacters[charName] = characterInfo;
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue loading other characters
                        System.Diagnostics.Debug.WriteLine(string.Format("Failed to load character {0}: {1}", charName, ex.Message));
                    }
                }
            }
            
            if (_availableCharacters.Count == 0)
            {
                Gremlin.ErrorClose("No valid characters found in SpriteSheet/Gremlins folder", "No Characters", true);
                return;
            }
        }
        
        /// <summary>
        /// Loads character configuration from config.txt
        /// </summary>
        private static CharacterInfo LoadCharacterConfig(string name, string configPath)
        {
            var character = new CharacterInfo(name);
            
            foreach (var line in File.ReadAllLines(configPath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//") || line.StartsWith("#") || !line.Contains("="))
                    continue;
                
                var parts = line.Split('=');
                if (parts.Length != 2) continue;
                
                string key = parts[0].Trim().ToUpper();
                string value = parts[1].Trim();
                
                switch (key)
                {
                    case "WIDTH":
                        if (int.TryParse(value, out int width))
                            character.FrameWidth = width;
                        break;
                    case "HEIGHT":
                        if (int.TryParse(value, out int height))
                            character.FrameHeight = height;
                        break;
                    case "COLUMN":
                        if (int.TryParse(value, out int column))
                            character.SpriteColumn = column;
                        break;
                    // Animation frame counts
                    case "INTRO":
                        if (int.TryParse(value, out int intro))
                            character.FrameCounts.Intro = intro;
                        break;
                    case "IDLE":
                        if (int.TryParse(value, out int idle))
                            character.FrameCounts.Idle = idle;
                        break;
                    case "IDLE2":
                        if (int.TryParse(value, out int idle2))
                            character.FrameCounts.Idle2 = idle2;
                        break;
                    case "OUTRO":
                        if (int.TryParse(value, out int outro))
                            character.FrameCounts.Outro = outro;
                        break;
                    case "RUNLEFT":
                    case "LEFT":
                        if (int.TryParse(value, out int left))
                            character.FrameCounts.Left = left;
                        break;
                    case "RUNRIGHT":
                    case "RIGHT":
                        if (int.TryParse(value, out int right))
                            character.FrameCounts.Right = right;
                        break;
                    case "RUNUP":
                    case "UP":
                        if (int.TryParse(value, out int up))
                            character.FrameCounts.Up = up;
                        break;
                    case "RUNDOWN":
                    case "DOWN":
                        if (int.TryParse(value, out int down))
                            character.FrameCounts.Down = down;
                        break;
                    case "UPLEFT":
                    case "UPRIGHT":
                    case "DOWNLEFT": 
                    case "DOWNRIGHT":
                        // Handle diagonal movements
                        if (int.TryParse(value, out int diag))
                        {
                            switch (key)
                            {
                                case "UPLEFT":
                                    character.FrameCounts.UpLeft = diag;
                                    break;
                                case "UPRIGHT":
                                    character.FrameCounts.UpRight = diag;
                                    break;
                                case "DOWNLEFT":
                                    character.FrameCounts.DownLeft = diag;
                                    break;
                                case "DOWNRIGHT":
                                    character.FrameCounts.DownRight = diag;
                                    break;
                            }
                        }
                        break;
                    case "GRAB":
                        if (int.TryParse(value, out int grab))
                            character.FrameCounts.Grab = grab;
                        break;
                    case "CLICK":
                        if (int.TryParse(value, out int click))
                            character.FrameCounts.Click = click;
                        break;
                    case "HOVER":
                        if (int.TryParse(value, out int hover))
                            character.FrameCounts.Hover = hover;
                        break;
                    case "SLEEP":
                        if (int.TryParse(value, out int sleep))
                            character.FrameCounts.Sleep = sleep;
                        break;
                    case "PAT":
                        if (int.TryParse(value, out int pat))
                            character.FrameCounts.Pat = pat;
                        break;
                    case "RUNIDLE":
                        if (int.TryParse(value, out int runIdle))
                            character.FrameCounts.RunIdle = runIdle;
                        break;
                    case "WALKLEFT":
                    case "WALK_L":
                        if (int.TryParse(value, out int walkL))
                            character.FrameCounts.WalkL = walkL;
                        break;
                    case "WALKRIGHT":
                    case "WALK_R":
                        if (int.TryParse(value, out int walkR))
                            character.FrameCounts.WalkR = walkR;
                        break;
                    case "WALKUP":
                    case "WALK_U":
                        if (int.TryParse(value, out int walkUp))
                            character.FrameCounts.WalkUp = walkUp;
                        break;
                    case "WALKDOWN":
                    case "WALK_D":
                        if (int.TryParse(value, out int walkDown))
                            character.FrameCounts.WalkDown = walkDown;
                        break;
                    case "EMOTE1":
                        if (int.TryParse(value, out int emote1))
                            character.FrameCounts.Emote1 = emote1;
                        break;
                    case "EMOTE2":
                        if (int.TryParse(value, out int emote2))
                            character.FrameCounts.Emote2 = emote2;
                        break;
                    case "EMOTE3":
                        if (int.TryParse(value, out int emote3))
                            character.FrameCounts.Emote3 = emote3;
                        break;
                    case "EMOTE4":
                        if (int.TryParse(value, out int emote4))
                            character.FrameCounts.Emote4 = emote4;
                        break;
                    case "DANCE":
                        if (int.TryParse(value, out int dance))
                            character.FrameCounts.Dance = dance;
                        break;
                    case "JUMPSCARE":
                        if (int.TryParse(value, out int jumpScare))
                            character.FrameCounts.JumpScare = jumpScare;
                        break;
                    case "POOF":
                        if (int.TryParse(value, out int poof))
                            character.FrameCounts.Poof = poof;
                        break;
                }
            }
            
            return character;
        }
        
        /// <summary>
        /// Gets all available character names (excluding dance-only characters)
        /// </summary>
        public static List<string> GetAvailableCharacters()
        {
            // Exclude dance-only characters that should not be selectable as main character
            var danceOnlyCharacters = new[] { "Bakushin", "Pasa", "Teio" };
            return _availableCharacters.Keys
                .Where(x => !Array.Exists(danceOnlyCharacters, c => c == x))
                .OrderBy(x => x)
                .ToList();
        }
        
        /// <summary>
        /// Gets character info by name
        /// </summary>
        public static CharacterInfo GetCharacterInfo(string characterName)
        {
            return _availableCharacters.TryGetValue(characterName, out var info) ? info : null;
        }
        
        /// <summary>
        /// Switches to a different character
        /// </summary>
        public static bool SwitchCharacter(string characterName)
        {
            if (!_availableCharacters.ContainsKey(characterName))
            {
                return false;
            }
            
            _currentCharacter = characterName;
            var character = _availableCharacters[characterName];
            
            // Update global settings with character-specific values
            Settings.StartingChar = characterName;
            Settings.SpriteColumn = character.SpriteColumn;
            Settings.FrameWidth = character.FrameWidth;
            Settings.FrameHeight = character.FrameHeight;
            
            // Apply character-specific feature settings to prevent bleeding
            CharacterFeatureManager.ApplyCharacterSpecificSettings(characterName);
            
            // Update global frame counts
            UpdateGlobalFrameCounts(character.FrameCounts);
            
            return true;
        }
        
        /// <summary>
        /// Updates the global FrameCounts and CurrentFrames with character-specific values
        /// </summary>
        private static void UpdateGlobalFrameCounts(FrameCounts frameCounts)
        {
            // Update FrameCounts (this should be made static in SpriteVariables.cs)
            // For now, we'll need to access it through a global instance
            // This will need to be refactored when we modify the main code
        }
        
        /// <summary>
        /// Gets the current active character name
        /// </summary>
        public static string GetCurrentCharacter()
        {
            return _currentCharacter;
        }
        
        /// <summary>
        /// Checks if a character has a specific animation
        /// </summary>
        public static bool HasAnimation(string characterName, string animationType)
        {
            var character = GetCharacterInfo(characterName);
            if (character == null) return false;
            
            switch (animationType.ToLower())
            {
                case "idle":
                    return character.FrameCounts.Idle > 0;
                case "idle2":
                    return character.FrameCounts.Idle2 > 0;
                case "intro":
                    return character.FrameCounts.Intro > 0;
                case "outro":
                    return character.FrameCounts.Outro > 0;
                case "left":
                case "runleft":
                    return character.FrameCounts.Left > 0;
                case "right":
                case "runright":
                    return character.FrameCounts.Right > 0;
                case "up":
                case "runup":
                    return character.FrameCounts.Up > 0;
                case "down":
                case "rundown":
                    return character.FrameCounts.Down > 0;
                case "grab":
                    return character.FrameCounts.Grab > 0;
                case "click":
                    return character.FrameCounts.Click > 0;
                case "hover":
                    return character.FrameCounts.Hover > 0;
                case "sleep":
                    return character.FrameCounts.Sleep > 0;
                case "pat":
                    return character.FrameCounts.Pat > 0;
                case "emote1":
                    return character.FrameCounts.Emote1 > 0;
                case "emote2":
                    return character.FrameCounts.Emote2 > 0;
                case "emote3":
                    return character.FrameCounts.Emote3 > 0;
                case "emote4":
                    return character.FrameCounts.Emote4 > 0;
                default:
                    return false;
            }
        }
    }
}