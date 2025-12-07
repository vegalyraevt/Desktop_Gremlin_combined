using Koyuki;
using Mambo;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static ConfigManager;
namespace Desktop_Gremlin
{
    public partial class Gremlin : Window
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);  
        private DateTime _nextRandomActionTime = DateTime.Now.AddSeconds(1);
        private Random _rng = new Random();
        private bool _wasIdleLastFrame = false;
        private DispatcherTimer _followTimer;
        private Target _currentFood;
        private AppConfig _config;
        private MediaPlayer _walkLoopPlayer;
        private DispatcherTimer _masterTimer;
        private DispatcherTimer _idleTimer;
        private DispatcherTimer _activeRandomMoveTimer;
        
        // Combat mode for Exusiai
        private bool _isCombatMode = false;

        // Music detection for dance
        private DateTime _nextDanceCheckTime = DateTime.MinValue;
        private DateTime _danceCooldownUntil = DateTime.MinValue;
        private bool _wasAudioPlayingLastCheck = false;
        private bool _isDanceAutoTriggered = false;  // Track if dance was auto-started by music detection
        private int _lastDanceFrame = -1;  // Track dance frame to detect cycle completion
        
        // Uma easter egg - multiple dancing characters
        private bool _isUmaPartyActive = false;
        private bool _wasUmaSongPlaying = false;
        private DateTime _nextUmaSongCheckTime = DateTime.MinValue;
        private const int UMA_CHECK_INTERVAL_SECONDS = 3;  // Check for Uma songs every 3 seconds
        private System.Collections.Generic.List<Window> _umaPartyWindows = new System.Collections.Generic.List<Window>();
        
        // Race tracking for Uma party
        private class RaceRunner
        {
            public string CharacterName { get; set; }
            public Window Window { get; set; }
            public Window PositionLabel { get; set; }  // Shows race position
            public int LapCount { get; set; }
            public double CurrentSpeed { get; set; }
            public double BaseSpeed { get; set; }
            public DateTime NextSpeedChange { get; set; }
            public double EdgeProgress { get; set; }  // Track position for ranking
            public int CurrentEdge { get; set; }
        }
        private System.Collections.Generic.List<RaceRunner> _raceRunners = new System.Collections.Generic.List<RaceRunner>();
        private DispatcherTimer _racePositionTimer;

        private Companion _companionInstance;
        private AnimationStates GremlinState = new AnimationStates();
        private FrameCounts FrameCounts = new FrameCounts();
        private CurrentFrames CurrentFrames = new CurrentFrames();
        public struct POINT
        {
            public int X;
            public int Y;
        }
        public Gremlin()
        {
            InitializeComponent();
            ConfigManager.LoadMasterConfig();
            
            // Load user settings
            GremlinSettings.Load();
            
            // Initialize character system
            CharacterManager.Initialize();
            
            // Switch to the configured starting character
            if (!string.IsNullOrEmpty(Settings.StartingChar))
            {
                CharacterManager.SwitchCharacter(Settings.StartingChar);
            }
            else if (CharacterManager.GetAvailableCharacters().Any())
            {
                // Fallback to first available character
                CharacterManager.SwitchCharacter(CharacterManager.GetAvailableCharacters().First());
            }
            
            ConfigManager.ApplyXamlSettings(this);
            SpriteImage.Source = new CroppedBitmap();
            FrameCounts = ConfigManager.LoadConfigChar(Settings.StartingChar);
            InitializeAnimations();
            InitializeTimers();
            GremlinState.LockState();
            _config = new AppConfig(this, GremlinState);

            MediaManager.PlaySound("intro.wav", Settings.StartingChar);
        }
        public void InitializeTimers()
        {
            _idleTimer = new DispatcherTimer();
            _idleTimer.Interval = TimeSpan.FromSeconds(Settings.SleepTime);
            _idleTimer.Tick += IdleTimer_Tick;
            _idleTimer.Start();
            //_walkLoopPlayer = new MediaPlayer();
            //_walkLoopPlayer.Open(new Uri(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds", Settings.StartingChar, "steps.wav")));
            //_walkLoopPlayer.Volume = 1; 
            //_walkLoopPlayer.MediaEnded += (s, e) =>
            //{
            //    _walkLoopPlayer.Position = TimeSpan.Zero;
            //    _walkLoopPlayer.Play(); 
            //};
        }
        
        public static void ErrorClose(string errorMessage, string errorTitle, bool close)
        {
            if (Settings.AllowErrorMessages)
            {
                System.Windows.MessageBox.Show(errorMessage, errorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            if (close)
            {
                System.Windows.Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// Triggers dance animation - called from tray menu
        /// </summary>
        public void TriggerDance()
        {
            try
            {
                // Reset current states and trigger dance
                GremlinState.UnlockState();
                CurrentFrames.Dance = 0;
                _isDanceAutoTriggered = false;  // User-triggered, don't auto-stop
                _lastDanceFrame = -1;
                GremlinState.SetState("Dance");
                
                // Play dance sound if available (optional)
                try
                {
                    MediaManager.PlaySound("dance.wav", Settings.StartingChar);
                }
                catch
                {
                    // If no dance sound, use a default sound or none
                    MediaManager.PlaySound("mambo.wav", Settings.StartingChar);
                }
                
                GremlinState.LockState();
            }
            catch (Exception ex)
            {
                ErrorClose($"Failed to trigger dance: {ex.Message}", "Dance Error", false);
            }
        }
        
        /// <summary>
        /// Reloads the current character - called when character is switched
        /// </summary>
        public void ReloadCharacter()
        {
            try
            {
                // Stop current timers temporarily
                if (_masterTimer != null)
                {
                    _masterTimer.Stop();
                }

                // Reload character configuration
                FrameCounts = ConfigManager.LoadConfigChar(Settings.StartingChar);
                
                // Reset animation states
                GremlinState.Reset();
                CurrentFrames = new CurrentFrames();
                
                // Reset sprite image
                SpriteImage.Source = new CroppedBitmap();
                
                // Play intro for new character
                GremlinState.SetIntro(true);
                MediaManager.PlaySound("intro.wav", Settings.StartingChar);
                
                // Restart timers
                if (_masterTimer != null)
                {
                    _masterTimer.Start();
                }
            }
            catch (Exception ex)
            {
                ErrorClose($"Failed to reload character: {ex.Message}", "Character Reload Error", false);
            }
        }
        
        /// <summary>
        /// Toggles combat mode for Exusiai (switches between normal and gun version)
        /// </summary>
        public void ToggleCombatMode()
        {
            // Only works for Exusiai
            if (Settings.StartingChar != "Exusiai" && Settings.StartingChar != "Exusiai_Gun")
            {
                return;
            }
            
            _isCombatMode = !_isCombatMode;
            
            // Switch between Exusiai and Exusiai_Gun
            if (Settings.StartingChar == "Exusiai")
            {
                Settings.StartingChar = "Exusiai_Gun";
            }
            else
            {
                Settings.StartingChar = "Exusiai_Gun";
                Settings.StartingChar = "Exusiai";
            }
            
            // Reload the character with new sprites
            FrameCounts = ConfigManager.LoadConfigChar(Settings.StartingChar);
            GremlinState.UnlockState();
            GremlinState.SetIntro(true);
            CurrentFrames.Idle = 0;
            CurrentFrames.Intro = 0;
            MediaManager.PlaySound("intro.wav", Settings.StartingChar);
        }
        
        /// <summary>
        /// Check if currently in combat mode
        /// </summary>
        public bool IsCombatMode => _isCombatMode;
        
        private int PlayAnimationIfActive(string stateName, string folder, int currentFrame, int frameCount, bool resetOnEnd)
        {
            if(!GremlinState.GetState(stateName))
            {
                return currentFrame;
            };
            currentFrame = SpriteManager.PlayAnimation(stateName, folder, currentFrame, frameCount, SpriteImage);


            if (resetOnEnd && currentFrame == 0 && stateName == "Outro")
            {
                System.Windows.Application.Current.Shutdown();  
            }

            if (resetOnEnd && currentFrame == 0)
            {
                GremlinState.UnlockState();
                GremlinState.ResetAllExceptIdle();
            }
            return currentFrame;
        }

        private void InitializeAnimations()
        {
            // Safety: ensure FrameRate is never 0 to avoid divide by zero
            int frameRate = Settings.FrameRate > 0 ? Settings.FrameRate : 30;
            _masterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000.0 / frameRate) };
            _masterTimer.Tick += (s, e) =>
            {
                //Repeatable Animations = false at the end//    
                CurrentFrames.Grab = PlayAnimationIfActive("Grab", "Actions", CurrentFrames.Grab, FrameCounts.Grab, false);
                CurrentFrames.Emote1 = PlayAnimationIfActive("Emote1", "Emotes", CurrentFrames.Emote1, FrameCounts.Emote1, false);
                CurrentFrames.Emote3 = PlayAnimationIfActive("Emote3", "Emotes", CurrentFrames.Emote3, FrameCounts.Emote3, false);
                CurrentFrames.Idle = PlayAnimationIfActive("Idle", "Actions", CurrentFrames.Idle, FrameCounts.Idle, false);
                CurrentFrames.Hover = PlayAnimationIfActive("Hover", "Actions", CurrentFrames.Hover, FrameCounts.Hover, false);
                CurrentFrames.Sleep = PlayAnimationIfActive("Sleeping", "Actions", CurrentFrames.Sleep, FrameCounts.Sleep, false);
                CurrentFrames.Pat = PlayAnimationIfActive("Pat", "Actions", CurrentFrames.Pat, FrameCounts.Pat, false);

                //Single Repeat Animations = true at the end//    
                CurrentFrames.Emote4 = PlayAnimationIfActive("Emote4", "Emotes", CurrentFrames.Emote4, FrameCounts.Emote4, true);
                CurrentFrames.Emote2 = PlayAnimationIfActive("Emote2", "Emotes", CurrentFrames.Emote2, FrameCounts.Emote2, true);
                
                // Dance with auto-stop logic for music-triggered dances
                if (GremlinState.GetState("Dance"))
                {
                    // Show music notes while dancing
                    if (GremlinSettings.MusicNotesEnabled && !MusicNoteOverlay.IsShowing && FrameCounts.Dance > 0)
                    {
                        MusicNoteOverlay.Show(this);
                    }
                    else if (!GremlinSettings.MusicNotesEnabled && MusicNoteOverlay.IsShowing)
                    {
                        MusicNoteOverlay.Hide();
                    }
                    MusicNoteOverlay.UpdateTargetPosition(this);
                    
                    int prevFrame = CurrentFrames.Dance;
                    CurrentFrames.Dance = SpriteManager.PlayAnimation("Dance", "Actions", CurrentFrames.Dance, FrameCounts.Dance, SpriteImage);
                    
                    // Detect cycle completion (frame wrapped from end back to start)
                    // Never auto-stop during Uma party - keep dancing until party ends!
                    if (GremlinSettings.AutoStopEnabled && _isDanceAutoTriggered && !_isUmaPartyActive && prevFrame > 0 && CurrentFrames.Dance == 0)
                    {
                        // Configurable chance to stop dancing after each cycle
                        int stopRoll = _rng.Next(100);
                        int effectiveStopChance = GremlinSettings.EffectiveStopChance;
                        DebugOverlay.Log("DANCE", $"Cycle complete! Stop roll: {stopRoll}/100 (need <{effectiveStopChance}" +
                            (GremlinSettings.DynamicChanceEnabled ? $", bonus: +{GremlinSettings.CurrentStopChanceBonus}%" : "") + " to stop)");
                        
                        if (stopRoll < effectiveStopChance)
                        {
                            DebugOverlay.Log("DANCE", "Auto-stopping dance");
                            
                            // Reset stop bonus on successful stop
                            if (GremlinSettings.DynamicChanceEnabled)
                                GremlinSettings.CurrentStopChanceBonus = 0;
                                
                            _isDanceAutoTriggered = false;
                            MusicNoteOverlay.Hide();
                            GremlinState.UnlockState();
                            GremlinState.ResetAllExceptIdle();
                        }
                        else
                        {
                            // Increment stop bonus on failed roll if dynamic chance enabled
                            if (GremlinSettings.DynamicChanceEnabled)
                                GremlinSettings.CurrentStopChanceBonus += GremlinSettings.StopChanceIncrement;
                        }
                    }
                }
                else
                {
                    // Hide music notes when not dancing
                    if (MusicNoteOverlay.IsShowing)
                    {
                        MusicNoteOverlay.Hide();
                    }
                }
                
                CurrentFrames.Intro = PlayAnimationIfActive("Intro", "Actions", CurrentFrames.Intro, FrameCounts.Intro,true);
                CurrentFrames.Outro = PlayAnimationIfActive("Outro", "Actions", CurrentFrames.Outro, FrameCounts.Outro, true);
                CurrentFrames.Click = PlayAnimationIfActive("Click", "Actions", CurrentFrames.Click, FrameCounts.Click, true);  
                
                // Check if character is stationary (doesn't move at all)
                bool isStationary = CharacterFeatureManager.IsStationary(Settings.StartingChar);
                bool isLeftRightOnly = CharacterFeatureManager.IsLeftRightOnly(Settings.StartingChar);
                
                if (!isStationary && MouseSettings.FollowCursor && GremlinState.GetState("Walking"))
                {
                    POINT cursorPos;
                    GetCursorPos(out cursorPos);
                    var cursorScreen = new System.Windows.Point(cursorPos.X, cursorPos.Y);

                    double halfW = SpriteImage.ActualWidth > 0 ? SpriteImage.ActualWidth / 2.0 : Settings.FrameWidth / 2.0;
                    double halfH = SpriteImage.ActualHeight > 0 ? SpriteImage.ActualHeight / 2.0 : Settings.FrameHeight / 2.0;
                    var spriteCenterScreen = SpriteImage.PointToScreen(new System.Windows.Point(halfW, halfH));


                    var source = PresentationSource.FromVisual(this);
                    System.Windows.Media.Matrix transformFromDevice = System.Windows.Media.Matrix.Identity;

                    if (source?.CompositionTarget != null)
                    {
                        transformFromDevice = source.CompositionTarget.TransformFromDevice;
                    }

                    var spriteCenterWpf = transformFromDevice.Transform(spriteCenterScreen);
                    var cursorWpf = transformFromDevice.Transform(cursorScreen);

                    double dx = cursorWpf.X - spriteCenterWpf.X;
                    double dy = cursorWpf.Y - spriteCenterWpf.Y;
                    
                    // For left/right only characters, ignore vertical movement
                    if (isLeftRightOnly)
                    {
                        dy = 0;
                    }
                    
                    double distance = Math.Sqrt(dx * dx + dy * dy);

                    if (distance > Settings.FollowRadius)
                    {
                        double step = Math.Min(MouseSettings.Speed, distance - Settings.FollowRadius);
                        double nx = dx / distance;
                        double ny = dy / distance;
                        double moveX = nx * step;
                        double moveY = isLeftRightOnly ? 0 : ny * step;  // No vertical movement for left/right only

                        this.Left += moveX;
                        this.Top += moveY;
                        double angle = Math.Atan2(moveY, moveX) * (180.0 / Math.PI);

                        if (angle < 0) angle += 360;

                        // For left/right only characters, simplify direction
                        if (isLeftRightOnly)
                        {
                            if (dx > 0)
                            {
                                CurrentFrames.Right = SpriteManager.PlayAnimation("runRight","Run",CurrentFrames.Right,FrameCounts.Right,SpriteImage);
                            }
                            else
                            {
                                CurrentFrames.Left = SpriteManager.PlayAnimation("runLeft","Run",CurrentFrames.Left,FrameCounts.Left,SpriteImage);
                            }
                        }
                        else if (angle >= 337.5 || angle < 22.5)
                        {
                            CurrentFrames.Right = SpriteManager.PlayAnimation("runRight","Run",CurrentFrames.Right,FrameCounts.Right,SpriteImage);
                        }
                        else if (angle >= 22.5 && angle < 67.5)
                        {
                            CurrentFrames.DownRight = SpriteManager.PlayAnimation("downRight","Run",CurrentFrames.DownRight,FrameCounts.DownRight,SpriteImage);
                        }
                        else if (angle >= 67.5 && angle < 112.5)
                        {
                            CurrentFrames.Down = SpriteManager.PlayAnimation("runDown","Run",CurrentFrames.Down,FrameCounts.Down,SpriteImage);
                        }
                        else if (angle >= 112.5 && angle < 157.5)
                        {
                            CurrentFrames.DownLeft = SpriteManager.PlayAnimation("downLeft","Run",CurrentFrames.DownLeft,FrameCounts.DownLeft,SpriteImage);
                        }
                        else if (angle >= 157.5 && angle < 202.5)
                        {
                            CurrentFrames.Left = SpriteManager.PlayAnimation("runLeft","Run",CurrentFrames.Left,FrameCounts.Left,SpriteImage);
                        }
                        else if (angle >= 202.5 && angle < 247.5)
                        {
                            CurrentFrames.UpLeft = SpriteManager.PlayAnimation("upLeft","Run",CurrentFrames.UpLeft,FrameCounts.UpLeft,SpriteImage);
                        }
                        else if (angle >= 247.5 && angle < 292.5)
                        {
                            CurrentFrames.Up = SpriteManager.PlayAnimation("runUp","Run",CurrentFrames.Up,FrameCounts.Up,SpriteImage);
                        }
                        else if (angle >= 292.5 && angle < 337.5)
                        {
                            CurrentFrames.UpRight = SpriteManager.PlayAnimation("upRight","Run",CurrentFrames.UpRight,FrameCounts.UpRight,SpriteImage);
                        }
                    }
                    else
                    {
                        CurrentFrames.WalkIdle = SpriteManager.PlayAnimation("runIdle","Actions",CurrentFrames.WalkIdle,FrameCounts.RunIdle,SpriteImage);
                    }

                }
                bool isIdleNow = GremlinState.IsCompletelyIdle();
                
                // Update debug overlay with current state
                DebugOverlay.UpdateState(GremlinState.GetCurrentState(), GremlinState.GetActiveAnimation());
                
                // Uma Party check - runs independently of dance check, even during sleep!
                // This is a special easter egg that overrides normal state restrictions
                if (GremlinSettings.UmaEasterEggEnabled && !_isUmaPartyActive)
                {
                    // Only check every 3 seconds to avoid spamming the media API
                    if (DateTime.Now >= _nextUmaSongCheckTime)
                    {
                        _nextUmaSongCheckTime = DateTime.Now.AddSeconds(UMA_CHECK_INTERVAL_SECONDS);
                        
                        // Update media info
                        AudioDetector.UpdateMediaInfoAsync();
                        
                        // Check if current song is Uma
                        bool isMusicPlaying = AudioDetector.IsMusicPlaying(
                            GremlinSettings.UseMediaSession, 
                            GremlinSettings.UseVolumeDetection, 
                            GremlinSettings.AudioThreshold);
                        
                        if (isMusicPlaying)
                        {
                            AudioDetector.CheckForUmaSong();
                            bool isUmaSong = AudioDetector.IsUmaSongPlaying;
                            
                            if (isUmaSong && !_wasUmaSongPlaying)
                            {
                                DebugOverlay.Log("UMA", $"🎠 Uma song detected: {AudioDetector.DetectedUmaSong}");
                                TriggerUmaParty();
                            }
                            _wasUmaSongPlaying = isUmaSong;
                        }
                        else if (_wasUmaSongPlaying)
                        {
                            // Music stopped
                            _wasUmaSongPlaying = false;
                        }
                    }
                }
                else if (_isUmaPartyActive)
                {
                    // Check if Uma party should end (song changed or stopped)
                    // Only check every few seconds
                    if (DateTime.Now >= _nextUmaSongCheckTime)
                    {
                        _nextUmaSongCheckTime = DateTime.Now.AddSeconds(UMA_CHECK_INTERVAL_SECONDS);
                        
                        bool isMusicPlaying = AudioDetector.IsMusicPlaying(
                            GremlinSettings.UseMediaSession, 
                            GremlinSettings.UseVolumeDetection, 
                            GremlinSettings.AudioThreshold);
                        
                        if (isMusicPlaying)
                        {
                            AudioDetector.CheckForUmaSong();
                            if (!AudioDetector.IsUmaSongPlaying)
                            {
                                DebugOverlay.Log("UMA", "🎠 Song changed - party over!");
                                EndUmaParty();
                                _wasUmaSongPlaying = false;
                            }
                        }
                        else
                        {
                            DebugOverlay.Log("UMA", "🎠 Music stopped - party over!");
                            EndUmaParty();
                            _wasUmaSongPlaying = false;
                        }
                    }
                }
                
                // Music detection for dance-capable characters
                if (GremlinSettings.AutoDanceEnabled && isIdleNow && DateTime.Now >= _nextDanceCheckTime && DateTime.Now >= _danceCooldownUntil)
                {
                    _nextDanceCheckTime = DateTime.Now.AddSeconds(GremlinSettings.DanceCheckIntervalSeconds);
                    
                    bool hasDance = CharacterFeatureManager.HasDanceAnimation(Settings.StartingChar);
                    if (hasDance && FrameCounts.Dance > 0)
                    {
                        // Use improved music detection (sustained audio check)
                        float audioLevel = AudioDetector.GetAverageAudioLevel();
                        bool isMusicPlaying = AudioDetector.IsMusicPlaying(
                            GremlinSettings.UseMediaSession, 
                            GremlinSettings.UseVolumeDetection, 
                            GremlinSettings.AudioThreshold);
                        
                        // Update debug with audio info
                        DebugOverlay.UpdateAudio(audioLevel, isMusicPlaying);
                        
                        // Check for dance when music is playing (skip if Uma party already active)
                        if (isMusicPlaying && !_isUmaPartyActive)
                        {
                            // Log only on transition to playing
                            if (!_wasAudioPlayingLastCheck)
                            {
                                DebugOverlay.Log("AUDIO", $"Music started! Avg Level: {audioLevel:P1}");
                            }
                            
                            // Roll for dance chance each check cycle while music is playing
                            int roll = _rng.Next(100);
                            int effectiveChance = GremlinSettings.EffectiveDanceChance;
                            if (roll < effectiveChance)
                            {
                                DebugOverlay.LogDanceTrigger($"Dance triggered! Roll: {roll}/100 (needed <{effectiveChance}" + 
                                    (GremlinSettings.DynamicChanceEnabled ? $", bonus: +{GremlinSettings.CurrentDanceChanceBonus}%" : "") + ")");
                                
                                // Reset dance bonus on successful trigger
                                if (GremlinSettings.DynamicChanceEnabled)
                                    GremlinSettings.CurrentDanceChanceBonus = 0;
                                    
                                CurrentFrames.Dance = 0;
                                _isDanceAutoTriggered = true;  // Mark as auto-triggered
                                _lastDanceFrame = -1;
                                GremlinState.UnlockState();
                                GremlinState.SetState("Dance");
                                GremlinState.LockState();
                                
                                // Cooldown before next dance check
                                _danceCooldownUntil = DateTime.Now.AddSeconds(GremlinSettings.DanceCooldownSeconds);
                            }
                            else
                            {
                                // Increment dance bonus on failed roll if dynamic chance enabled
                                if (GremlinSettings.DynamicChanceEnabled)
                                    GremlinSettings.CurrentDanceChanceBonus += GremlinSettings.DanceChanceIncrement;
                                    
                                DebugOverlay.Log("AUDIO", $"No dance - roll {roll}/100 (needed <{effectiveChance}" +
                                    (GremlinSettings.DynamicChanceEnabled ? $", next bonus: +{GremlinSettings.CurrentDanceChanceBonus}%" : "") + ")");
                            }
                        }
                        else if (_wasAudioPlayingLastCheck && !isMusicPlaying)
                        {
                            // Music stopped - reset dynamic bonus
                            if (GremlinSettings.DynamicChanceEnabled && GremlinSettings.CurrentDanceChanceBonus > 0)
                            {
                                DebugOverlay.Log("AUDIO", "Music stopped - resetting dance bonus");
                                GremlinSettings.CurrentDanceChanceBonus = 0;
                            }
                            
                            // Uma party ending is now handled in the separate Uma check block above
                        }
                        _wasAudioPlayingLastCheck = isMusicPlaying;
                    }
                }
                
                if (Settings.AllowRandomness)
                {
                    // Check if character is stationary
                    bool isStationaryChar = CharacterFeatureManager.IsStationary(Settings.StartingChar);
                    
                    if (isIdleNow && !_wasIdleLastFrame)
                    {
                        int interval = _rng.Next(Settings.RandomMinInterval, Settings.RandomMaxInterval);
                        _nextRandomActionTime = DateTime.Now.AddSeconds(interval);
                    }
                    if (isIdleNow && DateTime.Now >= _nextRandomActionTime)
                    {
                        GremlinState.SetState("Random");

                        // Stationary characters only do emote-type actions, not movement
                        if (isStationaryChar)
                        {
                            // Only do click/emote animation for stationary chars
                            CurrentFrames.Click = 0;
                            GremlinState.UnlockState();
                            GremlinState.SetState("Click");
                            MediaManager.PlaySound("mambo.wav",Settings.StartingChar);
                            GremlinState.LockState();
                        }
                        else
                        {
                            int action = _rng.Next(0, 5);
                            switch (action)
                            {
                                case 0:
                                    CurrentFrames.Click = 0;
                                    GremlinState.UnlockState();
                                    GremlinState.SetState("Click");
                                    MediaManager.PlaySound("mambo.wav",Settings.StartingChar);
                                    GremlinState.LockState();
                                    break;
                                case 1:
                                    RandomMove();
                                    break;
                                case 2:
                                    RandomMove();
                                    break;
                                case 3:
                                    RandomMove();
                                    break;
                                case 4:
                                    RandomMove();
                                    break;                            
                            }
                        }

                        int intervalAfterAction = _rng.Next(Settings.RandomMinInterval, Settings.RandomMaxInterval);
                        _nextRandomActionTime = DateTime.Now.AddSeconds(intervalAfterAction);
                    }
                }
                _wasIdleLastFrame = isIdleNow;
            };      
            _masterTimer.Start();     
        }
        private void RandomMove()
        {
            _activeRandomMoveTimer?.Stop();
            GremlinState.SetState("Random");

            double moveX = (_rng.NextDouble() - 0.5) * Settings.MoveDistance * 2;
            double moveY = (_rng.NextDouble() - 0.5) * Settings.MoveDistance * 2;

            double targetLeft = Math.Max(SystemParameters.WorkArea.Left,Math.Min(this.Left + moveX, SystemParameters.WorkArea.Right - SpriteImage.ActualWidth));
            double targetTop = Math.Max(SystemParameters.WorkArea.Top,Math.Min(this.Top + moveY, SystemParameters.WorkArea.Bottom - SpriteImage.ActualHeight));

            const double step = 120;
            double dx = (targetLeft - this.Left) / step;
            double dy = (targetTop - this.Top) / step;

            DispatcherTimer moveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
            _activeRandomMoveTimer = moveTimer;

            int moveCount = 0;

            moveTimer.Tick += (s, e) =>
            {
           
                this.Left += dx;
                this.Top += dy;
                moveCount++;
                if (Math.Abs(dx) > Math.Abs(dy))
                {
                    if (dx > 0)
                    {
                        CurrentFrames.WalkRight = SpriteManager.PlayAnimation("walkRight","Walk", CurrentFrames.WalkRight,FrameCounts.WalkR, SpriteImage);
                    }
                    else
                    {
                        CurrentFrames.WalkLeft = SpriteManager.PlayAnimation("walkLeft","Walk", CurrentFrames.WalkLeft,FrameCounts.WalkL, SpriteImage);
                    }
                }
                else
                {
                    if (dy > 0)
                    {
                        CurrentFrames.WalkDown = SpriteManager.PlayAnimation("walkDown","Walk", CurrentFrames.WalkDown,FrameCounts.WalkDown, SpriteImage);
                    }
                    else
                    {
                        CurrentFrames.WalkUp = SpriteManager.PlayAnimation("walkUp","Walk", CurrentFrames.WalkUp,FrameCounts.WalkUp,SpriteImage);
                    }
                }

                if (moveCount >= step || !GremlinState.GetState("Random") )
                {
                    moveTimer.Stop();
                    GremlinState.SetState("Idle");
                    _activeRandomMoveTimer = null;
                }
            };
            moveTimer.Start();
        }
   
        private void SpriteImage_RightClick(object sender, MouseButtonEventArgs e)
        {
            ResetIdleTimer();
            CurrentFrames.Click = 0;
            GremlinState.UnlockState();
            GremlinState.SetState("Click");
            MediaManager.PlaySound("mambo.wav", Settings.StartingChar);
            GremlinState.LockState();
        }

        private void SpriteImage_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Don't interrupt dance during Uma party
            if (_isUmaPartyActive) return;
            
            GremlinState.SetState("Hover");
            if (GremlinState.GetState("Hover"))
            {
                MediaManager.PlaySound("hover.wav",Settings.StartingChar, 5);
            }
            
        }
        private void SpriteImage_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Don't interrupt dance during Uma party
            if (_isUmaPartyActive) return;
            
            GremlinState.SetState("Idle");
            CurrentFrames.Hover = 0;            
        }
        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Don't interrupt dance during Uma party
            if (_isUmaPartyActive) return;
            
            ResetIdleTimer();
            
            // Check if character is stationary (like MamboFarmer)
            bool isStationary = CharacterFeatureManager.IsStationary(Settings.StartingChar);
            
            GremlinState.UnlockState();
            GremlinState.SetState("Grab");
            MediaManager.PlaySound("grab.wav", Settings.StartingChar);
            DragMove();
            
            // For stationary characters, just play click animation and return to idle
            if (isStationary)
            {
                CurrentFrames.Click = 0;
                GremlinState.SetState("Click");
                GremlinState.LockState();
                CurrentFrames.Grab = 0;
                return;
            }
            
            // Normal characters can toggle follow cursor mode
            GremlinState.SetState("Idle");
            MouseSettings.FollowCursor = !MouseSettings.FollowCursor;
            if (MouseSettings.FollowCursor)
            {
                GremlinState.SetState("Walking");
                GremlinState.LockState();
            }
            CurrentFrames.Grab = 0;
            if (MouseSettings.FollowCursor)
            {
                MediaManager.PlaySound("run.wav", Settings.StartingChar);
            }
        }
        private void TopHotspot_Click(object sender, MouseButtonEventArgs e)
        {
            // If companion is already showing, close it
            if (_companionInstance != null && _companionInstance.IsVisible)
            {
                _companionInstance.Close();
                DebugOverlay.UpdateCompanion(null);
                return;
            }
            
            // Check if companion feature is enabled in settings
            if (!GremlinSettings.CompanionEnabled)
            {
                // Companion disabled - just play a click sound
                MediaManager.PlaySound("click.wav", Settings.StartingChar);
                return;
            }
            
            // Check if current character supports companions
            if (!CharacterFeatureManager.SupportsCompanion(Settings.StartingChar))
            {
                // Character doesn't support companion - just play a click sound or do nothing
                MediaManager.PlaySound("click.wav", Settings.StartingChar);
                return;
            }
            
            // Get the appropriate companion for this character
            string companionName = CharacterFeatureManager.GetDefaultCompanion(Settings.StartingChar);
            if (string.IsNullOrEmpty(companionName))
            {
                return;
            }
            
            // Override the config companion with the character-specific one
            Settings.CompanionChar = companionName;
            
            _companionInstance = new Companion();
            _companionInstance.MainGremlin = this;
            _companionInstance.Closed += (s, args) => 
            {
                _companionInstance = null;
                DebugOverlay.UpdateCompanion(null);
            };
            _companionInstance.Show();
            DebugOverlay.UpdateCompanion(companionName);
        }
      
        private void ResetIdleTimer()
        {
            _idleTimer.Stop();
            _idleTimer.Start();
        }
        private void IdleTimer_Tick(object sender, EventArgs e)
        {
            if (GremlinState.GetState("Sleeping"))
            {
                return;
            }
            else
            {
                GremlinState.UnlockState();
                MediaManager.PlaySound("sleep.wav", Settings.StartingChar);
                GremlinState.SetState("Sleeping");
                GremlinState.LockState();
            }

        }
        public void EmoteHelper(string emote, string mp3)
        {
            ResetIdleTimer();
            GremlinState.UnlockState();
            GremlinState.SetState(emote);
            MediaManager.PlaySound(mp3,Settings.StartingChar);
            GremlinState.LockState();
        }
        private void LeftHotspot_Click(object sender, MouseButtonEventArgs e)
        {
       
            CurrentFrames.Emote1 = 0;
            EmoteHelper("Emote1", "emote1.wav");
        }
        private void LeftDownHotspot_Click(object sender, MouseButtonEventArgs e)
        {
            CurrentFrames.Emote2 = 0;
            EmoteHelper("Emote2", "emote2.wav");
        }
        private void RightHotspot_Click(object sender, MouseButtonEventArgs e)
        {
         
            CurrentFrames.Emote3 = 0;
            EmoteHelper("Emote3", "emote3.wav");

        }
        private void RightDownHotspot_Click(object sender, MouseButtonEventArgs e)
        {
            CurrentFrames.Emote4 = 0;
            EmoteHelper("Emote4", "emote4.wav");
        }
        
        #region Uma Easter Egg - Dancing Party
        
        // Dance-only characters (from jukebox, can only dance - no other animations)
        private static readonly string[] DanceOnlyCharacters = new[] { "Bakushin", "Pasa", "Teio" };
        
        // Uma Musume characters (only these should participate in Uma Party)
        // Excludes Blue Archive characters (Exusiai, Koyuki, etc.)
        private static readonly string[] UmaMusumeCharacters = new[] 
        { 
            "Agnes Tachyon", "Oguri", "RiceShower", "GoldShip", "Mambo", "Opera", "Doto", "Cafe",
            "Bakushin", "Pasa", "Teio"  // Dance-only Umas
        };
        
        /// <summary>
        /// Triggers the Uma party easter egg - spawns multiple dancing characters
        /// Special case: "Bakushin" song spawns only Bakushins!
        /// </summary>
        private void TriggerUmaParty()
        {
            if (_isUmaPartyActive) return;
            _isUmaPartyActive = true;
            _raceRunners.Clear();
            
            string detectedSong = AudioDetector.DetectedUmaSong ?? "";
            DebugOverlay.Log("UMA", $"Detected song: '{detectedSong}'");
            
            // Check for Bakushin song - various possible formats
            bool isBakushinSong = detectedSong.IndexOf("Bakushin", StringComparison.OrdinalIgnoreCase) >= 0 || 
                                  detectedSong.Contains("バクシン") ||
                                  detectedSong.IndexOf("bakushinshin", StringComparison.OrdinalIgnoreCase) >= 0;
            
            // Always make the main character dance if they can
            // Check both the feature flag AND if dance.png actually exists
            string mainDancePath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "SpriteSheet", "Gremlins", Settings.StartingChar, "dance.png");
            bool mainCanDance = System.IO.File.Exists(mainDancePath) && FrameCounts.Dance > 0;
            DebugOverlay.Log("UMA", $"Main char {Settings.StartingChar} canDance={mainCanDance}, dancePath exists={System.IO.File.Exists(mainDancePath)}, FrameCounts.Dance={FrameCounts.Dance}");
            
            if (mainCanDance)
            {
                CurrentFrames.Dance = 0;
                _isDanceAutoTriggered = true;
                GremlinState.UnlockState();
                GremlinState.SetState("Dance");
                GremlinState.LockState();
                DebugOverlay.Log("UMA", $"🎵 Main character set to DANCE state, locked={GremlinState.IsLocked}, currentState={GremlinState.GetCurrentState()}");
                
                // Show music notes on main character
                if (GremlinSettings.MusicNotesEnabled)
                {
                    MusicNoteOverlay.Show(this);
                }
            }
            else
            {
                DebugOverlay.Log("UMA", $"⚠️ Main character cannot dance!");
            }
            
            // Get current companion name to exclude
            string currentCompanion = "";
            if (_companionInstance != null)
            {
                currentCompanion = _companionInstance.CharacterName ?? "";
            }
            
            // Special case: Bakushin song = only Bakushins!
            System.Collections.Generic.List<string> availableCharacters;
            int partySize;
            // Use instance _rng for better randomization (seeded once at startup)
            
            if (isBakushinSong)
            {
                // BAKUSHIN BAKUSHIN BAKUSHIN! Spawn 5 Bakushin dancers + normal runners!
                DebugOverlay.Log("UMA", $"🏇 BAKUSHIN MODE ACTIVATED! Song: '{detectedSong}'");
                availableCharacters = new System.Collections.Generic.List<string>();
                
                // Add 5 Bakushin dancers first
                int numBakushins = 5;
                for (int i = 0; i < numBakushins; i++)
                {
                    availableCharacters.Add("Bakushin");
                }
                
                // Add normal Uma runners (Bakushin can't run, only dance)
                string basePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SpriteSheet", "Gremlins");
                foreach (var charName in UmaMusumeCharacters)
                {
                    // Skip main char, companion, and dance-only characters
                    if (charName == Settings.StartingChar || charName == currentCompanion)
                        continue;
                    if (Array.Exists(DanceOnlyCharacters, c => c == charName))
                        continue;
                    
                    string charPath = System.IO.Path.Combine(basePath, charName);
                    bool canRun = System.IO.File.Exists(System.IO.Path.Combine(charPath, "walkR.png")) ||
                                  System.IO.File.Exists(System.IO.Path.Combine(charPath, "right.png")) ||
                                  System.IO.File.Exists(System.IO.Path.Combine(charPath, "Walk", "walkRight.png")) ||
                                  System.IO.File.Exists(System.IO.Path.Combine(charPath, "Run", "runRight.png"));
                    
                    if (canRun)
                    {
                        availableCharacters.Add(charName);
                    }
                }
                
                partySize = availableCharacters.Count;
                DebugOverlay.Log("UMA", $"Spawning {numBakushins} Bakushin dancers + {partySize - numBakushins} runners!");
            }
            else
            {
                // Normal Uma party - Only Uma Musume characters participate
                // Dance-only Umas dance, all other Umas run in the race
                
                string basePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SpriteSheet", "Gremlins");
                
                // Build list of dance-only characters (spawn as dancers)
                var danceOnlyList = new System.Collections.Generic.List<string>();
                // Build list of runners (all Umas that can run, except main char)
                var runnersList = new System.Collections.Generic.List<string>();
                
                foreach (var charName in UmaMusumeCharacters)
                {
                    // Skip main char and companion - they don't spawn as extras
                    if (charName == Settings.StartingChar || charName == currentCompanion)
                        continue;
                    
                    string charPath = System.IO.Path.Combine(basePath, charName);
                    
                    // Check if this is a dance-only character
                    if (Array.Exists(DanceOnlyCharacters, c => c == charName))
                    {
                        // Dance-only character - check if dance.png exists
                        string dancePath = System.IO.Path.Combine(charPath, "dance.png");
                        if (System.IO.File.Exists(dancePath))
                        {
                            danceOnlyList.Add(charName);
                        }
                    }
                    else
                    {
                        // Regular Uma - check if they can run (has any walk/run sprite)
                        bool canRun = System.IO.File.Exists(System.IO.Path.Combine(charPath, "walkR.png")) ||
                                      System.IO.File.Exists(System.IO.Path.Combine(charPath, "right.png")) ||
                                      System.IO.File.Exists(System.IO.Path.Combine(charPath, "Walk", "walkRight.png")) ||
                                      System.IO.File.Exists(System.IO.Path.Combine(charPath, "Run", "runRight.png"));
                        
                        if (canRun)
                        {
                            runnersList.Add(charName);
                        }
                    }
                }
                
                DebugOverlay.Log("UMA", $"Dance-only Umas: {danceOnlyList.Count}: [{string.Join(", ", danceOnlyList)}]");
                DebugOverlay.Log("UMA", $"Runner Umas: {runnersList.Count}: [{string.Join(", ", runnersList)}]");
                
                // Build final list: dance-only characters first (they dance), then all runners
                availableCharacters = new System.Collections.Generic.List<string>();
                availableCharacters.AddRange(danceOnlyList);  // These will dance
                availableCharacters.AddRange(runnersList);     // These will run
                
                partySize = availableCharacters.Count;
                
                DebugOverlay.Log("UMA", $"🎉 PARTY: {danceOnlyList.Count} dancers, {runnersList.Count} runners");
            }
            
            // Track how many are dancers vs runners
            int numDancers = 0;
            bool isBakushinMode = isBakushinSong;
            
            if (isBakushinMode)
            {
                numDancers = 5; // First 5 are Bakushin dancers, rest are runners
            }
            else
            {
                // Count dance-only characters in the list
                foreach (var charName in availableCharacters)
                {
                    if (Array.Exists(DanceOnlyCharacters, c => c == charName))
                    {
                        numDancers++;
                    }
                }
            }
            
            int dancerCount = 0;
            int runnerIndex = 0;
            
            // Get screen dimensions
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            
            // Spawn characters
            for (int i = 0; i < partySize; i++)
            {
                var charName = availableCharacters[i];
                
                Window partyWindow;
                
                // First numDancers characters dance, rest run
                bool shouldDance = i < numDancers;
                
                if (shouldDance)
                {
                    dancerCount++;
                    partyWindow = CreateDancingWindow(charName, screenWidth, screenHeight, dancerCount, numDancers + 1); // +1 for main
                    DebugOverlay.Log("UMA", $"Created dancer window for {charName}");
                }
                else
                {
                    // Create a race runner
                    var runner = new RaceRunner
                    {
                        CharacterName = charName,
                        LapCount = 0,
                        BaseSpeed = 8 + _rng.NextDouble() * 4,  // Faster: 8-12 base speed
                        NextSpeedChange = DateTime.Now.AddSeconds(2 + _rng.NextDouble() * 3)
                    };
                    runner.CurrentSpeed = runner.BaseSpeed;
                    
                    partyWindow = CreateRunningWindow(charName, screenWidth, screenHeight, runnerIndex, runner);
                    DebugOverlay.Log("UMA", $"Created runner window for {charName}");
                    runnerIndex++;
                    if (partyWindow != null)
                    {
                        runner.Window = partyWindow;
                        _raceRunners.Add(runner);
                    }
                }
                
                if (partyWindow != null)
                {
                    _umaPartyWindows.Add(partyWindow);
                    partyWindow.Show();
                }
            }
            
            // Create position labels for racers and start position update timer
            if (_raceRunners.Count > 0)
            {
                foreach (var runner in _raceRunners)
                {
                    runner.PositionLabel = CreatePositionLabel(runner);
                    runner.PositionLabel?.Show();
                }
                
                // Start timer to update positions
                _racePositionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
                _racePositionTimer.Tick += UpdateRacePositions;
                _racePositionTimer.Start();
            }
            
            DebugOverlay.Log("UMA", $"🎠 Party started with {_umaPartyWindows.Count + 1} characters" + 
                (_raceRunners.Count > 0 ? $" ({_raceRunners.Count} racing!)" : "") + "!");
        }
        
        /// <summary>
        /// Creates a floating position label for a race runner
        /// </summary>
        private Window CreatePositionLabel(RaceRunner runner)
        {
            var label = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                Topmost = true,
                Width = 50,
                Height = 50,
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize,
                IsHitTestVisible = false
            };
            
            var border = new System.Windows.Controls.Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(20),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(220, 255, 215, 0)), // Gold
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 90, 0)),
                BorderThickness = new Thickness(2),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            var text = new System.Windows.Controls.TextBlock
            {
                Text = "1",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.DarkRed,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            border.Child = text;
            label.Content = border;
            label.Tag = text; // Store reference to update text
            
            // Position above the runner
            if (runner.Window != null)
            {
                label.Left = runner.Window.Left + 130;
                label.Top = runner.Window.Top - 30;
            }
            
            return label;
        }
        
        /// <summary>
        /// Updates race positions based on laps and progress
        /// </summary>
        private void UpdateRacePositions(object sender, EventArgs e)
        {
            if (_raceRunners.Count == 0) return;
            
            const int startEdge = 2; // All runners start at bottom edge
            
            // Sort runners by total distance traveled (laps + edges from start + progress)
            // Uses same formula as winner determination for consistency
            var sorted = _raceRunners.OrderByDescending(r => 
            {
                int edgesFromStart = (r.CurrentEdge - startEdge + 4) % 4;
                return (r.LapCount * 4.0) + edgesFromStart + r.EdgeProgress;
            }).ToList();
            
            for (int i = 0; i < sorted.Count; i++)
            {
                var runner = sorted[i];
                int position = i + 1;
                
                // Update position label
                if (runner.PositionLabel != null && runner.Window != null)
                {
                    runner.PositionLabel.Left = runner.Window.Left + 130;
                    runner.PositionLabel.Top = runner.Window.Top - 30;
                    
                    if (runner.PositionLabel.Tag is System.Windows.Controls.TextBlock text)
                    {
                        text.Text = position.ToString();
                        
                        // Color based on position
                        switch (position)
                        {
                            case 1:
                                text.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 215, 0)); // Gold
                                break;
                            case 2:
                                text.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(192, 192, 192)); // Silver
                                break;
                            case 3:
                                text.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(205, 127, 50)); // Bronze
                                break;
                            default:
                                text.Foreground = System.Windows.Media.Brushes.White;
                                break;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Creates a simple window that displays a dancing character
        /// </summary>
        private Window CreateDancingWindow(string characterName, double screenWidth, double screenHeight, int index, int total)
        {
            try
            {
                // First, load the dance sprite sheet to validate it exists
                var spritePath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "SpriteSheet", "Gremlins", characterName, "dance.png");
                
                if (!System.IO.File.Exists(spritePath))
                {
                    DebugOverlay.Log("UMA", $"No dance.png found for {characterName}");
                    return null;
                }
                
                var spriteSheet = new System.Windows.Media.Imaging.BitmapImage(new Uri(spritePath));
                
                // Get character-specific frame dimensions from config
                var charInfo = CharacterManager.GetCharacterInfo(characterName);
                int frameWidth = charInfo?.FrameWidth ?? 300;
                int frameHeight = charInfo?.FrameHeight ?? 300;
                int spriteColumns = charInfo?.SpriteColumn ?? 10;
                int danceFrameCount = charInfo?.FrameCounts?.Dance ?? 0;
                int frameCount = danceFrameCount;
                int columns = spriteColumns;
                
                // Check if this is a dance-only character (need to read config separately)
                bool isDanceOnly = Array.Exists(DanceOnlyCharacters, c => c == characterName);
                
                if (isDanceOnly)
                {
                    // Read from config.txt for dance-only characters (they use different config format)
                    var configPath = System.IO.Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "SpriteSheet", "Gremlins", characterName, "config.txt");
                    if (System.IO.File.Exists(configPath))
                    {
                        foreach (var line in System.IO.File.ReadAllLines(configPath))
                        {
                            string upperLine = line.ToUpper().Trim();
                            if (upperLine.StartsWith("DANCE_FRAME_COUNT=") || upperLine.StartsWith("DANCE="))
                            {
                                string val = line.Substring(line.IndexOf('=') + 1).Trim();
                                int.TryParse(val, out frameCount);
                            }
                            else if (upperLine.StartsWith("FRAME_WIDTH=") || upperLine.StartsWith("WIDTH="))
                            {
                                string val = line.Substring(line.IndexOf('=') + 1).Trim();
                                int.TryParse(val, out frameWidth);
                            }
                            else if (upperLine.StartsWith("FRAME_HEIGHT=") || upperLine.StartsWith("HEIGHT="))
                            {
                                string val = line.Substring(line.IndexOf('=') + 1).Trim();
                                int.TryParse(val, out frameHeight);
                            }
                            else if (upperLine.StartsWith("COLUMN=") || upperLine.StartsWith("COLUMNS="))
                            {
                                string val = line.Substring(line.IndexOf('=') + 1).Trim();
                                int.TryParse(val, out columns);
                            }
                        }
                    }
                    
                    // If no frame count in config, calculate from sprite sheet
                    if (frameCount <= 0 && frameWidth > 0)
                    {
                        // Calculate columns from sprite width
                        if (columns <= 0)
                        {
                            columns = spriteSheet.PixelWidth / frameWidth;
                            if (columns <= 0) columns = 10;
                        }
                        // Calculate total frames from sprite dimensions
                        int rows = spriteSheet.PixelHeight / frameHeight;
                        if (rows <= 0) rows = 1;
                        frameCount = columns * rows;
                    }
                    
                    DebugOverlay.Log("UMA", $"Dance-only {characterName}: {frameWidth}x{frameHeight}, cols={columns}, frames={frameCount}");
                }
                
                DebugOverlay.Log("UMA", $"Dancer {characterName}: {frameWidth}x{frameHeight}, cols={columns}, danceFrames={frameCount}");
                
                // Use fixed display size similar to runners for consistent appearance
                const double DANCER_DISPLAY_SIZE = 180;  // Slightly larger than runners (150)
                double windowWidth = DANCER_DISPLAY_SIZE;
                double windowHeight = DANCER_DISPLAY_SIZE;
                
                var window = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    Topmost = true,
                    Width = windowWidth,
                    Height = windowHeight,
                    ShowInTaskbar = false,
                    ResizeMode = ResizeMode.NoResize
                };
                
                // Position randomly across the bottom portion of screen
                // Use _rng for true randomization (not seeded by index)
                double left = _rng.NextDouble() * (screenWidth - windowWidth - 100) + 50;
                double top = screenHeight - windowHeight - 100 - _rng.NextDouble() * 150;
                
                window.Left = Math.Max(0, Math.Min(screenWidth - windowWidth, left));
                window.Top = Math.Max(0, Math.Min(screenHeight - windowHeight, top));
                
                // Create an image - use Stretch.Uniform to scale down while keeping aspect ratio
                var image = new System.Windows.Controls.Image
                {
                    Width = DANCER_DISPLAY_SIZE,
                    Height = DANCER_DISPLAY_SIZE,
                    Stretch = System.Windows.Media.Stretch.Uniform,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center
                };
                
                window.Content = image;
                
                // Calculate actual frame count from sprite sheet dimensions (most reliable)
                if (columns <= 0) columns = 10; // Default columns
                
                int sheetCols = spriteSheet.PixelWidth / frameWidth;
                int sheetRows = spriteSheet.PixelHeight / frameHeight;
                int actualFrameCount = sheetCols * sheetRows;
                
                // Use config frame count if valid and less than actual, otherwise use actual
                if (frameCount <= 0 || frameCount > actualFrameCount)
                {
                    frameCount = actualFrameCount;
                }
                
                // Use sheet columns for animation
                columns = sheetCols > 0 ? sheetCols : columns;
                
                if (frameCount <= 0) frameCount = 1; // Safety fallback
                
                DebugOverlay.Log("UMA", $"Dancer {characterName}: sheet={spriteSheet.PixelWidth}x{spriteSheet.PixelHeight}, frame={frameWidth}x{frameHeight}, cols={columns}, frames={frameCount}");
                
                int currentFrame = 0; // Start at first frame for consistency
                
                // Use consistent 60ms frame interval (~16fps) for smooth animation
                // This looks good for most dance animations
                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(60) };
                timer.Tick += (s, e) =>
                {
                    // Calculate frame position in sprite sheet (grid layout)
                    int col = currentFrame % columns;
                    int row = currentFrame / columns;
                    int x = col * frameWidth;
                    int y = row * frameHeight;
                    
                    // Crop and display frame
                    try
                    {
                        var croppedBitmap = new System.Windows.Media.Imaging.CroppedBitmap(
                            spriteSheet,
                            new System.Windows.Int32Rect(x, y, frameWidth, frameHeight));
                        
                        image.Source = croppedBitmap;
                    }
                    catch { /* Ignore cropping errors */ }
                    
                    // Loop animation
                    currentFrame = (currentFrame + 1) % frameCount;
                };
                timer.Start();
                
                // Store timer reference so we can stop it later
                window.Tag = timer;
                
                // Create a simple music note overlay for this window
                // (We'll create individual overlays rather than using the singleton)
                var noteWindow = CreateMusicNoteWindow(window);
                if (noteWindow != null)
                {
                    noteWindow.Show();
                    window.Closed += (s, e) => noteWindow.Close();
                }
                
                return window;
            }
            catch (Exception ex)
            {
                DebugOverlay.Log("UMA", $"Failed to create dancer: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Creates a simple music note overlay window for a party character
        /// </summary>
        private Window CreateMusicNoteWindow(Window targetWindow)
        {
            try
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SpriteSheet", "System", "music_note.png");
                if (!System.IO.File.Exists(path)) return null;
                
                var spriteSheet = new BitmapImage();
                spriteSheet.BeginInit();
                spriteSheet.UriSource = new Uri(path);
                spriteSheet.CacheOption = BitmapCacheOption.OnLoad;
                spriteSheet.EndInit();
                spriteSheet.Freeze();
                
                var noteWindow = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    Topmost = true,
                    Width = 100,
                    Height = 100,
                    ShowInTaskbar = false,
                    ResizeMode = ResizeMode.NoResize
                };
                
                var noteImage = new System.Windows.Controls.Image
                {
                    Width = 100,
                    Height = 100,
                    Stretch = Stretch.Uniform
                };
                noteWindow.Content = noteImage;
                
                // Position relative to target window - same as main character's music notes
                noteWindow.Left = targetWindow.Left + targetWindow.Width - 60;
                noteWindow.Top = targetWindow.Top + 30;
                
                // Animate the music notes
                int noteFrame = 0;
                const int FRAME_WIDTH = 300;
                const int FRAME_HEIGHT = 300;
                const int SPRITE_COLUMN = 5;
                const int FRAME_COUNT = 123;
                
                var noteTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
                noteTimer.Tick += (s, e) =>
                {
                    int x = (noteFrame % SPRITE_COLUMN) * FRAME_WIDTH;
                    int y = (noteFrame / SPRITE_COLUMN) * FRAME_HEIGHT;
                    
                    if (x + FRAME_WIDTH <= spriteSheet.PixelWidth && y + FRAME_HEIGHT <= spriteSheet.PixelHeight)
                    {
                        noteImage.Source = new CroppedBitmap(spriteSheet, new Int32Rect(x, y, FRAME_WIDTH, FRAME_HEIGHT));
                    }
                    
                    noteFrame = (noteFrame + 1) % FRAME_COUNT;
                    
                    // Follow target window - same as main character's music notes
                    noteWindow.Left = targetWindow.Left + targetWindow.Width - 60;
                    noteWindow.Top = targetWindow.Top + 30;
                };
                noteTimer.Start();
                
                noteWindow.Closed += (s, e) => noteTimer.Stop();
                
                return noteWindow;
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Creates a window with a character that runs around the screen edges like a race
        /// </summary>
        private Window CreateRunningWindow(string characterName, double screenWidth, double screenHeight, int index, RaceRunner runner)
        {
            try
            {
                // Get character-specific frame dimensions for sprite cropping
                var charInfo = CharacterManager.GetCharacterInfo(characterName);
                int frameWidth = charInfo?.FrameWidth ?? 300;
                int frameHeight = charInfo?.FrameHeight ?? 300;
                int spriteColumns = charInfo?.SpriteColumn ?? 1;
                // Get frame count - try walk first, then run, then default
                int walkFrameCount = charInfo?.FrameCounts?.WalkR ?? 0;
                if (walkFrameCount <= 0)
                    walkFrameCount = charInfo?.FrameCounts?.Right ?? 0;  // Run animation
                if (walkFrameCount <= 0)
                    walkFrameCount = 4;  // Default fallback
                
                // Use FIXED smaller window size for all runners (makes them uniform and smaller)
                const double RUNNER_DISPLAY_SIZE = 150;
                double windowWidth = RUNNER_DISPLAY_SIZE;
                double windowHeight = RUNNER_DISPLAY_SIZE;
                
                // Create the window
                var window = new Window
                {
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = System.Windows.Media.Brushes.Transparent,
                    Topmost = true,
                    Width = windowWidth,
                    Height = windowHeight,
                    ShowInTaskbar = false,
                    ResizeMode = ResizeMode.NoResize
                };
                
                // All runners start at the SAME position - bottom center (like a starting line)
                // They'll spread out as the race progresses due to different speeds
                int startEdge = 2; // Bottom edge
                double startProgress = 0.5; // Center of bottom edge
                double startX = startProgress * (screenWidth - windowWidth);
                double startY = screenHeight - windowHeight;
                
                // Slight offset based on index so they're not perfectly stacked
                startX += (index - 3) * 20; // Spread slightly at start
                
                window.Left = Math.Max(0, Math.Min(screenWidth - windowWidth, startX));
                window.Top = startY;
                
                // Create an image - use Stretch.Uniform to scale down while keeping aspect ratio
                var image = new System.Windows.Controls.Image
                {
                    Width = RUNNER_DISPLAY_SIZE,
                    Height = RUNNER_DISPLAY_SIZE,
                    Stretch = System.Windows.Media.Stretch.Uniform,  // Scale to fit uniformly
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center
                };
                
                window.Content = image;
                
                // Try to load walk/run sprite - check multiple locations
                string basePath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "SpriteSheet", "Gremlins", characterName);
                
                // List of possible sprite paths to try, in order of preference
                string[] possibleRightPaths = new[]
                {
                    // Flat structure (Agnes Tachyon, Oguri, etc.)
                    System.IO.Path.Combine(basePath, "walkR.png"),
                    System.IO.Path.Combine(basePath, "right.png"),
                    // Subdirectory structure (Doto, Mambo, Opera)
                    System.IO.Path.Combine(basePath, "Walk", "walkRight.png"),
                    System.IO.Path.Combine(basePath, "Run", "runRight.png"),
                    // Fallback to idle
                    System.IO.Path.Combine(basePath, "Actions", "idle.png"),
                    System.IO.Path.Combine(basePath, "idle.png")
                };
                
                string spritePath = null;
                foreach (var path in possibleRightPaths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        spritePath = path;
                        break;
                    }
                }
                
                if (spritePath == null)
                {
                    DebugOverlay.Log("UMA", $"No sprite found for runner {characterName}");
                    return null;
                }
                
                DebugOverlay.Log("UMA", $"Runner {characterName} using sprite: {System.IO.Path.GetFileName(spritePath)}");
                
                var spriteSheet = new System.Windows.Media.Imaging.BitmapImage(new Uri(spritePath));
                
                // Movement state - all start at bottom center
                int currentFrame = 0;
                int frameCount = walkFrameCount;
                int currentEdge = startEdge; // 2 = bottom edge
                int startingEdge = startEdge; // Remember start for lap counting
                bool movingClockwise = true; // All race the same direction (left around screen)
                double edgeProgress = 0.5 + (index * 0.02); // Slight offset at start
                bool passedStart = false; // For lap counting
                
                // Glow effect state
                DateTime glowEndTime = DateTime.MinValue;
                bool isGlowingBlue = false;
                bool isGlowingRed = false;
                
                // Preload both directions - check multiple locations for left sprite
                var spriteSheetLeft = spriteSheet;
                string[] possibleLeftPaths = new[]
                {
                    // Flat structure (Agnes Tachyon, Oguri, etc.)
                    System.IO.Path.Combine(basePath, "walkL.png"),
                    System.IO.Path.Combine(basePath, "left.png"),
                    // Subdirectory structure (Doto, Mambo, Opera)
                    System.IO.Path.Combine(basePath, "Walk", "walkLeft.png"),
                    System.IO.Path.Combine(basePath, "Run", "runLeft.png")
                };
                
                foreach (var path in possibleLeftPaths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        spriteSheetLeft = new System.Windows.Media.Imaging.BitmapImage(new Uri(path));
                        break;
                    }
                }
                
                // Calculate grid layout for sprite sheet
                int columns = spriteColumns > 0 ? spriteColumns : 1;
                
                var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
                timer.Tick += (s, e) =>
                {
                    // Variable speed - change speed periodically
                    if (DateTime.Now >= runner.NextSpeedChange)
                    {
                        double oldSpeed = runner.CurrentSpeed;
                        // Randomly speed up or slow down (0.5x to 1.5x base speed)
                        double speedMultiplier = 0.5 + _rng.NextDouble() * 1.0;
                        runner.CurrentSpeed = runner.BaseSpeed * speedMultiplier;
                        runner.NextSpeedChange = DateTime.Now.AddSeconds(1 + _rng.NextDouble() * 2);
                        
                        // Trigger glow effect if significant speed change
                        double speedChange = runner.CurrentSpeed / oldSpeed;
                        if (speedChange > 1.3) // Speed boost - blue glow
                        {
                            isGlowingBlue = true;
                            isGlowingRed = false;
                            glowEndTime = DateTime.Now.AddMilliseconds(500);
                        }
                        else if (speedChange < 0.7) // Speed debuff - red glow
                        {
                            isGlowingRed = true;
                            isGlowingBlue = false;
                            glowEndTime = DateTime.Now.AddMilliseconds(500);
                        }
                    }
                    
                    // Update glow effect
                    if (DateTime.Now >= glowEndTime)
                    {
                        isGlowingBlue = false;
                        isGlowingRed = false;
                        image.Effect = null;
                    }
                    else if (isGlowingBlue)
                    {
                        image.Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            Color = System.Windows.Media.Colors.Cyan,
                            BlurRadius = 20,
                            ShadowDepth = 0,
                            Opacity = 0.9
                        };
                    }
                    else if (isGlowingRed)
                    {
                        image.Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            Color = System.Windows.Media.Colors.Red,
                            BlurRadius = 20,
                            ShadowDepth = 0,
                            Opacity = 0.9
                        };
                    }
                    
                    // Update position along edge using runner's current speed
                    double edgeDelta = runner.CurrentSpeed / 500.0; // Normalize speed
                    edgeProgress += edgeDelta;
                    
                    // Update runner tracking for position display
                    runner.EdgeProgress = edgeProgress;
                    runner.CurrentEdge = currentEdge;
                    
                    // Check for edge transition
                    if (edgeProgress > 1.0)
                    {
                        edgeProgress = 0;
                        int prevEdge = currentEdge;
                        currentEdge = (currentEdge + 1) % 4;
                        
                        // Check if we completed a lap (passed starting edge)
                        if (currentEdge == startingEdge && !passedStart)
                        {
                            runner.LapCount++;
                            passedStart = true;
                            DebugOverlay.Log("RACE", $"🏇 {characterName} completed lap {runner.LapCount}!");
                        }
                        else if (currentEdge != startingEdge)
                        {
                            passedStart = false;
                        }
                    }
                    
                    // Calculate position based on current edge
                    // Use fixed runner size for position calculations
                    const double runnerSize = 150; // RUNNER_DISPLAY_SIZE
                    double newX = 0, newY = 0;
                    bool facingRight = true;
                    
                    switch (currentEdge)
                    {
                        case 0: // Top edge - moving right or left
                            newX = edgeProgress * (screenWidth - runnerSize);
                            newY = 0;
                            facingRight = movingClockwise;
                            break;
                        case 1: // Right edge - moving down or up
                            newX = screenWidth - runnerSize;
                            newY = edgeProgress * (screenHeight - runnerSize);
                            facingRight = false; // Facing into screen
                            break;
                        case 2: // Bottom edge - moving left or right
                            newX = (1 - edgeProgress) * (screenWidth - runnerSize);
                            newY = screenHeight - runnerSize;
                            facingRight = !movingClockwise;
                            break;
                        case 3: // Left edge - moving up or down
                            newX = 0;
                            newY = (1 - edgeProgress) * (screenHeight - runnerSize);
                            facingRight = true; // Facing into screen
                            break;
                    }
                    
                    window.Left = newX;
                    window.Top = newY;
                    
                    // Select correct sprite sheet based on direction
                    var activeSheet = facingRight ? spriteSheet : spriteSheetLeft;
                    
                    // Calculate frame position in sprite sheet (grid layout: col x row)
                    int col = currentFrame % columns;
                    int row = currentFrame / columns;
                    int x = col * frameWidth;
                    int y = row * frameHeight;
                    
                    // Bounds check and crop
                    if (x + frameWidth <= activeSheet.PixelWidth && y + frameHeight <= activeSheet.PixelHeight)
                    {
                        var croppedBitmap = new System.Windows.Media.Imaging.CroppedBitmap(
                            activeSheet,
                            new System.Windows.Int32Rect(x, y, frameWidth, frameHeight));
                        
                        image.Source = croppedBitmap;
                    }
                    
                    currentFrame = (currentFrame + 1) % frameCount;
                };
                timer.Start();
                
                // Store timer reference
                window.Tag = timer;
                
                return window;
            }
            catch (Exception ex)
            {
                DebugOverlay.Log("UMA", $"Failed to create runner: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Ends the Uma party and cleans up spawned windows
        /// </summary>
        private void EndUmaParty()
        {
            // Find the race winner before cleaning up
            // Winner is whoever has traveled the furthest total distance (laps + current position)
            RaceRunner winner = null;
            double winnerDistance = -1;
            const int startEdge = 2; // All runners start at bottom edge
            
            if (_raceRunners.Count > 0)
            {
                foreach (var runner in _raceRunners)
                {
                    // Calculate edges traveled from start (clockwise: 2→3→0→1→2)
                    // This correctly handles the wrap-around from edge 3 to edge 0
                    int edgesFromStart = (runner.CurrentEdge - startEdge + 4) % 4;
                    double totalDistance = (runner.LapCount * 4.0) + edgesFromStart + runner.EdgeProgress;
                    
                    DebugOverlay.Log("RACE", $"{runner.CharacterName}: Lap {runner.LapCount}, Edge {runner.CurrentEdge}, Progress {runner.EdgeProgress:F2}, Total: {totalDistance:F2}");
                    
                    if (totalDistance > winnerDistance)
                    {
                        winner = runner;
                        winnerDistance = totalDistance;
                    }
                }
                
                DebugOverlay.Log("RACE", $"🏆 Race ended! Winner: {winner?.CharacterName} with distance {winnerDistance:F2} ({winner?.LapCount} laps)");
            }
            
            _isUmaPartyActive = false;
            _wasUmaSongPlaying = false;
            
            // Stop position update timer
            if (_racePositionTimer != null)
            {
                _racePositionTimer.Stop();
                _racePositionTimer = null;
            }
            
            // Close position labels
            foreach (var runner in _raceRunners)
            {
                try { runner.PositionLabel?.Close(); } catch { }
            }
            
            // Close all party windows
            foreach (var window in _umaPartyWindows)
            {
                try
                {
                    // Stop the animation timer
                    if (window.Tag is DispatcherTimer timer)
                    {
                        timer.Stop();
                    }
                    window.Close();
                }
                catch { }
            }
            _umaPartyWindows.Clear();
            
            // Stop main character dancing (optional - let it finish naturally)
            // Hide music notes
            MusicNoteOverlay.Hide();
            
            // Show winner if there was a race!
            if (winner != null && winner.LapCount > 0)
            {
                ShowRaceWinner(winner);
            }
            else
            {
                DebugOverlay.Log("UMA", "🎠 Party ended, everyone went home!");
            }
            
            _raceRunners.Clear();
        }
        
        /// <summary>
        /// Shows a popup announcing the race winner with first place graphic and character
        /// </summary>
        private void ShowRaceWinner(RaceRunner winner)
        {
            DebugOverlay.Log("RACE", $"🏆 WINNER: {winner.CharacterName} with {winner.LapCount} laps!");
            
            // Play winner intro voice - every runner has intro.wav and it's perfect for victory!
            MediaManager.PlaySound("intro.wav", winner.CharacterName);
            
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            
            // Create winner announcement window (positioned in corner)
            var winnerWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                Topmost = true,
                Width = 500,
                Height = 350,
                ShowInTaskbar = false,
                ResizeMode = ResizeMode.NoResize,
                Left = screenWidth - 520,
                Top = 20
            };
            
            var mainGrid = new System.Windows.Controls.Grid();
            
            // Background panel
            var bgBorder = new System.Windows.Controls.Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(230, 30, 30, 50)),
                CornerRadius = new CornerRadius(15),
                BorderBrush = new System.Windows.Media.LinearGradientBrush(
                    System.Windows.Media.Color.FromRgb(255, 215, 0),
                    System.Windows.Media.Color.FromRgb(255, 180, 0),
                    45),
                BorderThickness = new Thickness(3)
            };
            
            var contentStack = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10)
            };
            
            // Load first place trophy image
            string trophyPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SpriteSheet", "System", "first_place.png");
            if (System.IO.File.Exists(trophyPath))
            {
                try
                {
                    var trophyBitmap = new BitmapImage();
                    trophyBitmap.BeginInit();
                    trophyBitmap.UriSource = new Uri(trophyPath);
                    trophyBitmap.CacheOption = BitmapCacheOption.OnLoad;
                    trophyBitmap.EndInit();
                    trophyBitmap.Freeze();
                    
                    var trophyImage = new System.Windows.Controls.Image
                    {
                        Source = trophyBitmap,
                        Width = 100,
                        Height = 100,
                        Stretch = Stretch.Uniform,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(10)
                    };
                    contentStack.Children.Add(trophyImage);
                }
                catch
                {
                    // Fallback to emoji if image fails
                    var trophyLabel = new System.Windows.Controls.TextBlock
                    {
                        Text = "🥇",
                        FontSize = 80,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(10)
                    };
                    contentStack.Children.Add(trophyLabel);
                }
            }
            else
            {
                // Fallback to emoji if image not found
                var trophyLabel = new System.Windows.Controls.TextBlock
                {
                    Text = "🥇",
                    FontSize = 80,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10)
                };
                contentStack.Children.Add(trophyLabel);
            }
            
            // Character info panel
            var infoStack = new System.Windows.Controls.StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10)
            };
            
            // Winner text
            var winnerLabel = new System.Windows.Controls.TextBlock
            {
                Text = "🏆 WINNER! 🏆",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 215, 0)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };
            infoStack.Children.Add(winnerLabel);
            
            // Character name
            var nameLabel = new System.Windows.Controls.TextBlock
            {
                Text = winner.CharacterName,
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };
            infoStack.Children.Add(nameLabel);
            
            // Lap count
            var lapLabel = new System.Windows.Controls.TextBlock
            {
                Text = $"{winner.LapCount} Lap{(winner.LapCount != 1 ? "s" : "")}!",
                FontSize = 18,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(144, 238, 144)),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            infoStack.Children.Add(lapLabel);
            
            contentStack.Children.Add(infoStack);
            
            // Try to load a random icon for the winner from the Icons folder
            string iconsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Icons");
            if (System.IO.Directory.Exists(iconsPath))
            {
                // Map character names to their icon prefixes
                var iconPrefixes = new System.Collections.Generic.Dictionary<string, string[]>
                {
                    { "Agnes Tachyon", new[] { "Tach1", "Tach2", "Tach3", "Tach_plush" } },
                    { "GoldShip", new[] { "gold1", "gold2", "gold3", "gold4", "Gold_plush1", "Gold_plush2" } },
                    { "Oguri", new[] { "Oguri", "Oguri_plush1", "Oguri_plush2" } },
                    { "RiceShower", new[] { "Rice", "Rice_plush" } },
                    { "Doto", new[] { "Doto", "Doto_plush" } },
                    { "Mambo", new[] { "Mambo", "Mambo2", "mambo_plush", "mambo_plush2" } },
                    { "Opera", new[] { "opera", "Opera_Plush" } },
                    { "Koyuki", new[] { "Koyuki" } },
                    { "Exusiai", new[] { "Exusiai" } },
                    { "Cafe", new[] { "cafe1", "cafe2", "cafe3", "cafe_plush" } }
                };
                
                string[] possibleIcons = null;
                if (iconPrefixes.TryGetValue(winner.CharacterName, out possibleIcons) && possibleIcons.Length > 0)
                {
                    // Pick a random icon
                    string selectedIcon = possibleIcons[_rng.Next(possibleIcons.Length)];
                    string iconPath = System.IO.Path.Combine(iconsPath, selectedIcon + ".ico");
                    
                    if (System.IO.File.Exists(iconPath))
                    {
                        try
                        {
                            var iconBitmap = new BitmapImage();
                            iconBitmap.BeginInit();
                            iconBitmap.UriSource = new Uri(iconPath);
                            iconBitmap.CacheOption = BitmapCacheOption.OnLoad;
                            iconBitmap.DecodePixelWidth = 128; // Scale down for display
                            iconBitmap.EndInit();
                            iconBitmap.Freeze();
                            
                            var charImage = new System.Windows.Controls.Image
                            {
                                Source = iconBitmap,
                                Width = 128,
                                Height = 128,
                                Stretch = Stretch.Uniform,
                                Margin = new Thickness(10)
                            };
                            contentStack.Children.Add(charImage);
                        }
                        catch { /* Ignore icon loading errors */ }
                    }
                }
            }
            
            bgBorder.Child = contentStack;
            mainGrid.Children.Add(bgBorder);
            winnerWindow.Content = mainGrid;
            
            winnerWindow.Show();
            
            // Auto-close after 4 seconds
            var closeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            closeTimer.Tick += (s, e) =>
            {
                closeTimer.Stop();
                winnerWindow.Close();
            };
            closeTimer.Start();
        }
        
        #endregion
    }
}
