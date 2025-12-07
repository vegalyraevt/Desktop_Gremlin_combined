using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Desktop_Gremlin
{
    /// <summary>
    /// Debug overlay window for monitoring animation states and triggers
    /// </summary>
    public class DebugOverlay : Window
    {
        private static DebugOverlay _instance;
        private static bool _isEnabled = false;
        private TextBlock _logText;
        private ScrollViewer _scrollViewer;
        private Queue<string> _logLines = new Queue<string>();
        private const int MaxLogLines = 50;
        
        // Stats
        private TextBlock _statsText;
        private string _currentState = "";
        private string _currentAnimation = "";
        private float _lastAudioLevel = 0f;
        private bool _lastAudioPlaying = false;
        private DateTime _lastDanceTime = DateTime.MinValue;

        public static new bool IsEnabled => _isEnabled;

        private DebugOverlay()
        {
            Title = "Desktop Gremlin Debug";
            Width = 450;
            Height = 680;
            WindowStyle = WindowStyle.ToolWindow;
            Topmost = true;
            Background = new SolidColorBrush(Color.FromArgb(230, 30, 30, 30));
            
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(360) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Stats panel
            var statsBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(255, 40, 40, 40)),
                Margin = new Thickness(5),
                Padding = new Thickness(10)
            };
            _statsText = new TextBlock
            {
                Foreground = Brushes.LightGreen,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                Text = "Initializing..."
            };
            statsBorder.Child = _statsText;
            Grid.SetRow(statsBorder, 0);
            grid.Children.Add(statsBorder);

            // Log panel
            _scrollViewer = new ScrollViewer
            {
                Margin = new Thickness(5),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            _logText = new TextBlock
            {
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                Padding = new Thickness(5)
            };
            _scrollViewer.Content = _logText;
            Grid.SetRow(_scrollViewer, 1);
            grid.Children.Add(_scrollViewer);

            Content = grid;

            // Prevent closing, just hide
            Closing += (s, e) =>
            {
                e.Cancel = true;
                Hide();
                _isEnabled = false;
            };

            // Update stats every 100ms
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            timer.Tick += (s, e) => UpdateStats();
            timer.Start();
        }

        public static void Toggle()
        {
            if (_instance == null)
            {
                _instance = new DebugOverlay();
            }

            _isEnabled = !_isEnabled;
            if (_isEnabled)
            {
                // Initialize audio detection systems to populate status
                AudioDetector.Initialize();
                AudioDetector.InitializeMediaSession();
                
                _instance.Show();
                Log("DEBUG", "Debug overlay enabled");
            }
            else
            {
                _instance.Hide();
            }
        }

        public static void Log(string category, string message)
        {
            if (_instance == null || !_isEnabled) return;

            _instance.Dispatcher.BeginInvoke(new Action(() =>
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                string line = $"[{timestamp}] [{category}] {message}";
                
                _instance._logLines.Enqueue(line);
                while (_instance._logLines.Count > MaxLogLines)
                {
                    _instance._logLines.Dequeue();
                }

                _instance._logText.Text = string.Join("\n", _instance._logLines);
                _instance._scrollViewer.ScrollToEnd();
            }));
        }

        public static void UpdateState(string state, string animation)
        {
            if (_instance == null) return;
            _instance._currentState = state;
            _instance._currentAnimation = animation;
        }

        public static void UpdateAudio(float level, bool isPlaying)
        {
            if (_instance == null) return;
            _instance._lastAudioLevel = level;
            _instance._lastAudioPlaying = isPlaying;
        }

        public static void LogDanceTrigger(string reason)
        {
            if (_instance == null) return;
            _instance._lastDanceTime = DateTime.Now;
            Log("DANCE", reason);
        }
        
        // Companion tracking
        private static string _companionName = "";
        
        public static void UpdateCompanion(string companionName)
        {
            _companionName = companionName ?? "";
        }

        private void UpdateStats()
        {
            string danceCooldown = _lastDanceTime == DateTime.MinValue 
                ? "Never" 
                : $"{(DateTime.Now - _lastDanceTime).TotalSeconds:F1}s ago";

            // Raw and adjusted audio levels
            float rawLevel = AudioDetector.LastRawLevel;
            float adjustedLevel = rawLevel * GremlinSettings.AudioSensitivity;
            float threshold = GremlinSettings.AudioThreshold;
            bool aboveThreshold = adjustedLevel > threshold;
            
            // Detection mode status
            string mediaMode = GremlinSettings.UseMediaSession ? "✓ ON" : "✗ OFF";
            string volMode = GremlinSettings.UseVolumeDetection ? "✓ ON" : "✗ OFF";
            string detectionUsed = AudioDetector.LastDetectionMethod;
            
            // Media session info
            string mediaAvail = AudioDetector.MediaSessionAvailable ? "Available" : "Unavailable";
            string mediaSource = !string.IsNullOrEmpty(AudioDetector.LastMediaSource) 
                ? AudioDetector.LastMediaSource : "None";
            string sessionError = !string.IsNullOrEmpty(AudioDetector.LastSessionError)
                ? AudioDetector.LastSessionError : "None";
            string activeSessions = !string.IsNullOrEmpty(AudioDetector.AllActiveSessions)
                ? AudioDetector.AllActiveSessions : "None";
            int sessionCount = AudioDetector.LastSessionCount;
            
            // Companion status
            string companionStatus = GremlinSettings.CompanionEnabled ? "Enabled" : "Disabled";
            string companionActive = !string.IsNullOrEmpty(_companionName) ? _companionName : "None";

            // Dynamic chance info
            string dynamicMode = GremlinSettings.DynamicChanceEnabled ? "ON" : "OFF";
            string danceChanceStr = GremlinSettings.DynamicChanceEnabled 
                ? $"{GremlinSettings.DanceChancePercent}%+{GremlinSettings.CurrentDanceChanceBonus}%={GremlinSettings.EffectiveDanceChance}%" 
                : $"{GremlinSettings.DanceChancePercent}%";
            string stopChanceStr = GremlinSettings.DynamicChanceEnabled 
                ? $"{GremlinSettings.StopChancePercent}%+{GremlinSettings.CurrentStopChanceBonus}%={GremlinSettings.EffectiveStopChance}%" 
                : $"{GremlinSettings.StopChancePercent}%";

            // Volume bar visualization
            int barLength = 20;
            int filledRaw = (int)(rawLevel * barLength);
            int filledAdj = (int)(Math.Min(1f, adjustedLevel) * barLength);
            int thresholdPos = (int)(threshold * barLength);
            string rawBar = new string('█', filledRaw) + new string('░', barLength - filledRaw);
            string adjBar = new string('█', filledAdj) + new string('░', barLength - filledAdj);

            _statsText.Text = $"Character: {Settings.StartingChar}\n" +
                             $"State: {_currentState} | Anim: {_currentAnimation}\n" +
                             $"───────────────────────────────\n" +
                             $"🎵 DANCE SETTINGS\n" +
                             $"   Auto-Dance: {(GremlinSettings.AutoDanceEnabled ? "ON" : "OFF")} | " +
                             $"Auto-Stop: {(GremlinSettings.AutoStopEnabled ? "ON" : "OFF")}\n" +
                             $"   Notes: {(GremlinSettings.MusicNotesEnabled ? "ON" : "OFF")} | " +
                             $"Dynamic: {dynamicMode}\n" +
                             $"   Dance Chance: {danceChanceStr}\n" +
                             $"   Stop Chance: {stopChanceStr}\n" +
                             $"───────────────────────────────\n" +
                             $"🎧 MUSIC DETECTION (Active: {detectionUsed})\n" +
                             $"   MediaSession: {mediaMode} | Volume: {volMode}\n" +
                             $"───────────────────────────────\n" +
                             $"📺 MEDIA SESSION ({mediaAvail})\n" +
                             $"   Sessions: {sessionCount} | Error: {sessionError}\n" +
                             $"   Active Apps: {activeSessions}\n" +
                             $"   Media Source: {mediaSource}\n" +
                             $"───────────────────────────────\n" +
                             $"🎵 NOW PLAYING\n" +
                             $"   App: {(string.IsNullOrEmpty(AudioDetector.CurrentMediaApp) ? "-" : AudioDetector.CurrentMediaApp)}\n" +
                             $"   {(string.IsNullOrEmpty(AudioDetector.CurrentMediaInfo) ? "No media info available" : AudioDetector.CurrentMediaInfo)}\n" +
                             $"───────────────────────────────\n" +
                             $"🔊 VOLUME DETECTION\n" +
                             $"   Raw:  [{rawBar}] {rawLevel:P0}\n" +
                             $"   Adj:  [{adjBar}] {adjustedLevel:P0}\n" +
                             $"   Threshold: {threshold:P0} | Above: {(aboveThreshold ? "YES" : "NO")}\n" +
                             $"   Sensitivity: x{GremlinSettings.AudioSensitivity:F1}\n" +
                             $"───────────────────────────────\n" +
                             $"👥 COMPANION\n" +
                             $"   Setting: {companionStatus} | Active: {companionActive}\n" +
                             $"───────────────────────────────\n" +
                             $"💃 Last Dance: {danceCooldown}";
        }
    }
}
