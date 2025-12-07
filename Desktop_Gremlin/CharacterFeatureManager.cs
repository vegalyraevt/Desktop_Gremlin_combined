using System;
using System.Collections.Generic;
using System.IO;

namespace Desktop_Gremlin
{
    /// <summary>
    /// Manages character-specific features and capabilities to prevent feature bleeding
    /// </summary>
    public static class CharacterFeatureManager
    {
        /// <summary>
        /// Character-specific feature flags
        /// </summary>
        public class CharacterFeatures
        {
            public bool HasFireMechanics { get; set; } = false;
            public bool HasAmmoSystem { get; set; } = false;
            public bool HasKeyboardControl { get; set; } = false;
            public bool HasWalkAnimations { get; set; } = false;
            public bool HasEmoteAnimations { get; set; } = false;
            public bool HasDiagonalMovement { get; set; } = false;
            public bool HasJumpScare { get; set; } = false;
            public bool HasSpecialEffects { get; set; } = false;
            public bool HasFootstepSounds { get; set; } = false;
            public bool HasGravityPhysics { get; set; } = false;
            public bool HasDanceAnimation { get; set; } = false;
            
            // Companion support - which character(s) can be a companion for this one
            public string DefaultCompanion { get; set; } = null;  // null = no companion support
            public string[] PossibleCompanions { get; set; } = null;  // For random selection (e.g., Opera can spawn Doto or Oguri)
            
            // Animation naming conventions
            public bool UsesLegacyAnimationNames { get; set; } = false; // Some chars use LEFT/RIGHT instead of RUNLEFT/RUNRIGHT
            public bool HasIntroOutro { get; set; } = false;
            public bool HasSleepAnimation { get; set; } = false;
            
            // Movement restrictions
            public bool IsLeftRightOnly { get; set; } = false;  // 2D side-scroller style (no up/down)
            public bool IsStationary { get; set; } = false;     // Character doesn't move at all (like MamboFarmer)
            public bool HasRunAnimations { get; set; } = true;  // Can run/move around
        }

        private static readonly Dictionary<string, CharacterFeatures> _characterFeatures = new Dictionary<string, CharacterFeatures>();

        /// <summary>
        /// Initialize character features based on their configs and known capabilities
        /// </summary>
        public static void InitializeCharacterFeatures()
        {
            _characterFeatures.Clear();
            
            // Define known character features based on their original versions
            DefineKnownCharacterFeatures();
            
            // Scan and auto-detect features from character configs
            var availableCharacters = CharacterManager.GetAvailableCharacters();
            foreach (var characterName in availableCharacters)
            {
                if (!_characterFeatures.ContainsKey(characterName))
                {
                    _characterFeatures[characterName] = DetectCharacterFeatures(characterName);
                }
            }
            
            // Also initialize dance-only characters (they're excluded from GetAvailableCharacters)
            var danceOnlyCharacters = new[] { "Bakushin", "Pasa", "Teio" };
            foreach (var characterName in danceOnlyCharacters)
            {
                if (!_characterFeatures.ContainsKey(characterName))
                {
                    _characterFeatures[characterName] = new CharacterFeatures
                    {
                        HasDanceAnimation = true,
                        HasRunAnimations = false,
                        IsStationary = true  // They only dance in place
                    };
                }
            }
        }

        private static void DefineKnownCharacterFeatures()
        {
            // Exusiai - 2D left/right only character
            _characterFeatures["Exusiai"] = new CharacterFeatures
            {
                HasWalkAnimations = true,
                HasEmoteAnimations = false,
                HasDiagonalMovement = false,
                HasIntroOutro = true,
                HasSleepAnimation = true,
                IsLeftRightOnly = true  // Only moves left/right along taskbar
            };

            // Exusiai with Gun - 2D left/right with fire mechanics
            _characterFeatures["Exusiai_Gun"] = new CharacterFeatures
            {
                HasFireMechanics = true,
                HasAmmoSystem = true,
                HasKeyboardControl = true,
                HasEmoteAnimations = true,
                HasIntroOutro = true,
                HasSpecialEffects = true,
                IsLeftRightOnly = true  // Only moves left/right along taskbar
            };

            // Mambo characters - Basic movement, no special mechanics
            _characterFeatures["Mambo"] = new CharacterFeatures
            {
                HasWalkAnimations = true,
                HasEmoteAnimations = true,
                HasDiagonalMovement = true,
                HasIntroOutro = true,
                HasSleepAnimation = true,
                DefaultCompanion = "Agnes Tachyon"  // Mambo + Agnes (same 300x300 size)
            };

            // MamboFarmer - Stationary character that only reacts to clicks
            _characterFeatures["MamboFarmer"] = new CharacterFeatures
            {
                HasWalkAnimations = false,
                HasDiagonalMovement = false,
                HasEmoteAnimations = true,
                HasRunAnimations = false,
                IsStationary = true,  // Doesn't move around
                HasIntroOutro = false
            };

            // Agnes Tachyon - Full character with dance animation (merged from Agnes + Tachyon dance)
            _characterFeatures["Agnes Tachyon"] = new CharacterFeatures
            {
                HasWalkAnimations = true,
                HasDiagonalMovement = true,
                UsesLegacyAnimationNames = true,
                HasIntroOutro = true,
                HasDanceAnimation = true,
                HasSleepAnimation = true,
                DefaultCompanion = "Cafe"  // Agnes + Cafe are a set pair
            };

            // Opera - Effects-based character, can spawn Doto or Oguri as companion
            _characterFeatures["Opera"] = new CharacterFeatures
            {
                HasSpecialEffects = true,
                HasDiagonalMovement = true,
                HasIntroOutro = true,
                PossibleCompanions = new[] { "Doto", "Oguri" }  // 50/50 random
            };

            // Doto - Full featured character with Opera companion
            _characterFeatures["Doto"] = new CharacterFeatures
            {
                HasWalkAnimations = true,
                HasEmoteAnimations = true,
                HasDiagonalMovement = true,
                HasIntroOutro = true,
                HasSleepAnimation = true,
                HasSpecialEffects = true,
                DefaultCompanion = "Opera"  // Doto + Opera is the original companion pair
            };

            // Oguri - Can have Opera as companion too
            _characterFeatures["Oguri"] = new CharacterFeatures
            {
                HasWalkAnimations = true,
                HasDiagonalMovement = true,
                UsesLegacyAnimationNames = true,
                HasIntroOutro = true,
                HasDanceAnimation = true,
                DefaultCompanion = "Opera"  // Oguri + Opera works well
            };

            // RiceShower - Full character with dance animation
            _characterFeatures["RiceShower"] = new CharacterFeatures
            {
                HasWalkAnimations = true,
                HasEmoteAnimations = true,
                HasDiagonalMovement = true,
                HasIntroOutro = true,
                HasSleepAnimation = true,
                HasDanceAnimation = true,
                UsesLegacyAnimationNames = true
            };

            // Other characters get basic features by default
            var basicFeatures = new CharacterFeatures
            {
                HasWalkAnimations = true,
                HasDiagonalMovement = true,
                HasIntroOutro = true
            };

            // Koyuki - Has run but no walk animations
            _characterFeatures["Koyuki"] = new CharacterFeatures
            {
                HasWalkAnimations = false,  // No walk sprites
                HasEmoteAnimations = true,
                HasDiagonalMovement = true,
                UsesLegacyAnimationNames = true,
                HasIntroOutro = true,
                HasSleepAnimation = true
            };
            
            // GoldShip - Walking focused
            _characterFeatures["GoldShip"] = new CharacterFeatures
            {
                HasWalkAnimations = true,
                HasDiagonalMovement = true,
                UsesLegacyAnimationNames = true,
                HasIntroOutro = true
            };
            
            // Manhattan Cafe - Full featured Uma Musume character
            _characterFeatures["Cafe"] = new CharacterFeatures
            {
                HasWalkAnimations = true,
                HasEmoteAnimations = true,
                HasDiagonalMovement = true,
                HasIntroOutro = true,
                HasSleepAnimation = true,
                HasRunAnimations = true,
                DefaultCompanion = "Agnes Tachyon"  // Cafe + Agnes Tachyon
            };
        }

        /// <summary>
        /// Auto-detect character features from their config files
        /// </summary>
        private static CharacterFeatures DetectCharacterFeatures(string characterName)
        {
            var features = new CharacterFeatures();
            
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "SpriteSheet", "Gremlins", characterName, "config.txt");

                if (!File.Exists(configPath))
                    return features;

                var configLines = File.ReadAllLines(configPath);
                
                foreach (var line in configLines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//") || !line.Contains("="))
                        continue;

                    var parts = line.Split('=');
                    if (parts.Length != 2) continue;

                    string key = parts[0].Trim().ToUpper();
                    string value = parts[1].Trim();

                    if (!int.TryParse(value, out int frameCount) || frameCount <= 0)
                        continue;

                    // Detect features based on animation presence
                    switch (key)
                    {
                        case "FIRELEFT":
                        case "FIRERIGHT":
                        case "LEFTFIRE":
                        case "RIGHTFIRE":
                            features.HasFireMechanics = true;
                            break;
                        case "RELOAD":
                            features.HasAmmoSystem = true;
                            features.HasFireMechanics = true;
                            break;
                        case "WALKLEFT":
                        case "WALKRIGHT":
                        case "WALKUP":
                        case "WALKDOWN":
                        case "WALK_L":
                        case "WALK_R":
                        case "WALK_U":
                        case "WALK_D":
                            features.HasWalkAnimations = true;
                            break;
                        case "EMOTE1":
                        case "EMOTE2":
                        case "EMOTE3":
                        case "EMOTE4":
                            features.HasEmoteAnimations = true;
                            break;
                        case "UPLEFT":
                        case "UPRIGHT":
                        case "DOWNLEFT":
                        case "DOWNRIGHT":
                            features.HasDiagonalMovement = true;
                            break;
                        case "INTRO":
                            features.HasIntroOutro = true;
                            break;
                        case "OUTRO":
                            features.HasIntroOutro = true;
                            break;
                        case "SLEEP":
                        case "SLEEPING":
                            features.HasSleepAnimation = true;
                            break;
                        case "JUMPSCARE":
                            features.HasJumpScare = true;
                            break;
                        case "DANCE":
                            features.HasDanceAnimation = true;
                            break;
                        case "POOF":
                        case "EFFECT1":
                        case "EFFECT2":
                            features.HasSpecialEffects = true;
                            break;
                        case "LEFT":
                        case "RIGHT":
                        case "UP":
                        case "DOWN":
                            // If character uses LEFT/RIGHT instead of RUNLEFT/RUNRIGHT
                            if (!HasRunAnimations(configLines))
                            {
                                features.UsesLegacyAnimationNames = true;
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Failed to detect features for {0}: {1}", characterName, ex.Message));
            }

            return features;
        }

        private static bool HasRunAnimations(string[] configLines)
        {
            foreach (var line in configLines)
            {
                if (line.ToUpper().Contains("RUNLEFT") || line.ToUpper().Contains("RUNRIGHT"))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get features for a specific character
        /// </summary>
        public static CharacterFeatures GetCharacterFeatures(string characterName)
        {
            return _characterFeatures.TryGetValue(characterName, out var features) 
                ? features 
                : new CharacterFeatures(); // Return basic features if unknown
        }

        /// <summary>
        /// Check if character supports a specific feature
        /// </summary>
        public static bool CharacterHasFeature(string characterName, string featureName)
        {
            var features = GetCharacterFeatures(characterName);
            
            switch (featureName.ToLower())
            {
                case "fire":
                case "firing":
                    return features.HasFireMechanics;
                case "ammo":
                    return features.HasAmmoSystem;
                case "keyboard":
                    return features.HasKeyboardControl;
                case "walk":
                    return features.HasWalkAnimations;
                case "emote":
                    return features.HasEmoteAnimations;
                case "diagonal":
                    return features.HasDiagonalMovement;
                case "jumpscare":
                    return features.HasJumpScare;
                case "effects":
                    return features.HasSpecialEffects;
                case "intro":
                    return features.HasIntroOutro;
                case "sleep":
                    return features.HasSleepAnimation;
                case "dance":
                    return features.HasDanceAnimation;
                case "leftright":
                case "leftrightonly":
                    return features.IsLeftRightOnly;
                case "stationary":
                    return features.IsStationary;
                case "run":
                    return features.HasRunAnimations;
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Check if character is restricted to left/right movement only
        /// </summary>
        public static bool IsLeftRightOnly(string characterName)
        {
            var features = GetCharacterFeatures(characterName);
            return features.IsLeftRightOnly;
        }
        
        /// <summary>
        /// Check if character is stationary (doesn't move)
        /// </summary>
        public static bool IsStationary(string characterName)
        {
            var features = GetCharacterFeatures(characterName);
            return features.IsStationary;
        }

        /// <summary>
        /// Check if character has dance animation
        /// </summary>
        public static bool HasDanceAnimation(string characterName)
        {
            var features = GetCharacterFeatures(characterName);
            return features.HasDanceAnimation;
        }
        
        /// <summary>
        /// Get the dance frame count for a character by reading the sprite sheet
        /// </summary>
        public static int GetDanceFrameCount(string characterName)
        {
            try
            {
                var spritePath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "SpriteSheet", "Gremlins", characterName, "dance.png");
                
                if (!System.IO.File.Exists(spritePath))
                    return 4; // Default
                    
                // Read config.txt for this character to get frame count
                var configPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "SpriteSheet", "Gremlins", characterName, "config.txt");
                
                if (System.IO.File.Exists(configPath))
                {
                    foreach (var line in System.IO.File.ReadAllLines(configPath))
                    {
                        string upperLine = line.ToUpper().Trim();
                        if (upperLine.StartsWith("DANCE="))
                        {
                            string value = line.Substring(line.IndexOf('=') + 1).Trim();
                            if (int.TryParse(value, out int count))
                                return count;
                        }
                    }
                }
                
                // Default frame counts for known characters
                switch (characterName)
                {
                    case "Agnes Tachyon": return 6;
                    case "RiceShower": return 4;
                    case "Oguri": return 4;
                    case "Doto": return 4;
                    case "GoldShip": return 4;
                    case "Mambo": return 4;
                    default: return 4;
                }
            }
            catch
            {
                return 4;
            }
        }

        /// <summary>
        /// Apply character-specific settings when switching characters
        /// </summary>
        public static void ApplyCharacterSpecificSettings(string characterName)
        {
            var features = GetCharacterFeatures(characterName);

            // Reset ammo system if character doesn't support it
            if (!features.HasAmmoSystem)
            {
                Settings.Ammo = 0;
                Settings.CurrentAmmo = 0;
            }
            else
            {
                // Set default ammo for characters that support it (like Exu)
                if (characterName.StartsWith("Exu"))
                {
                    Settings.Ammo = 30;
                    Settings.CurrentAmmo = 30;
                }
            }

            // Disable footstep sounds for characters that don't support them
            if (!features.HasFootstepSounds && !features.HasWalkAnimations)
            {
                Settings.FootStepSounds = false;
            }

            // Adjust physics settings
            if (!features.HasGravityPhysics)
            {
                Settings.AllowGravity = false;
            }
        }

        /// <summary>
        /// Get animation name with character-specific naming conventions
        /// </summary>
        public static string GetCharacterSpecificAnimationName(string characterName, string baseAnimationName)
        {
            var features = GetCharacterFeatures(characterName);
            
            if (features.UsesLegacyAnimationNames)
            {
                switch (baseAnimationName.ToLower())
                {
                    case "runleft":
                        return "left";
                    case "runright":
                        return "right";
                    case "runup":
                        return "up";
                    case "rundown":
                        return "down";
                    case "walkleft":
                        return "walk_l";
                    case "walkright":
                        return "walk_r";
                    case "walkup":
                        return "walk_u";
                    case "walkdown":
                        return "walk_d";
                    default:
                        return baseAnimationName;
                }
            }
            
            return baseAnimationName;
        }
        
        /// <summary>
        /// Check if a character supports having a companion
        /// </summary>
        public static bool SupportsCompanion(string characterName)
        {
            var features = GetCharacterFeatures(characterName);
            return !string.IsNullOrEmpty(features.DefaultCompanion) || 
                   (features.PossibleCompanions != null && features.PossibleCompanions.Length > 0);
        }
        
        /// <summary>
        /// Get the companion for a character (random if multiple options)
        /// </summary>
        private static Random _companionRng = new Random();
        public static string GetDefaultCompanion(string characterName)
        {
            var features = GetCharacterFeatures(characterName);
            
            // If there are multiple possible companions, pick randomly
            if (features.PossibleCompanions != null && features.PossibleCompanions.Length > 0)
            {
                int index = _companionRng.Next(features.PossibleCompanions.Length);
                return features.PossibleCompanions[index];
            }
            
            return features.DefaultCompanion;
        }
    }
}