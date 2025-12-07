using System;
using System.IO;
using System.Windows;

namespace Desktop_Gremlin
{
    public partial class SettingsWindow : Window
    {
        private static SettingsWindow _instance;
        
        public SettingsWindow()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        public static void ShowSettings()
        {
            if (_instance == null || !_instance.IsLoaded)
            {
                _instance = new SettingsWindow();
            }
            _instance.LoadCurrentSettings();
            _instance.Show();
            _instance.Activate();
        }

        private void LoadCurrentSettings()
        {
            // Dance settings
            chkAutoDance.IsChecked = GremlinSettings.AutoDanceEnabled;
            chkAutoStop.IsChecked = GremlinSettings.AutoStopEnabled;
            chkMusicNotes.IsChecked = GremlinSettings.MusicNotesEnabled;
            chkDynamicChance.IsChecked = GremlinSettings.DynamicChanceEnabled;
            sliderDanceChance.Value = GremlinSettings.DanceChancePercent;
            sliderDanceIncrement.Value = GremlinSettings.DanceChanceIncrement;
            sliderStopChance.Value = GremlinSettings.StopChancePercent;
            sliderStopIncrement.Value = GremlinSettings.StopChanceIncrement;
            sliderDanceCheckInterval.Value = GremlinSettings.DanceCheckIntervalSeconds;
            sliderDanceCooldown.Value = GremlinSettings.DanceCooldownSeconds;
            chkUmaEasterEgg.IsChecked = GremlinSettings.UmaEasterEggEnabled;
            
            // Audio/music detection settings
            chkUseMediaSession.IsChecked = GremlinSettings.UseMediaSession;
            chkUseVolumeDetection.IsChecked = GremlinSettings.UseVolumeDetection;
            sliderAudioThreshold.Value = GremlinSettings.AudioThresholdPercent;
            sliderAudioSensitivity.Value = GremlinSettings.AudioSensitivityMultiplier;
            sliderVolume.Value = GremlinSettings.SoundVolume;
            
            // Behavior settings
            chkAllowRandomness.IsChecked = Settings.AllowRandomness;
            chkFollowCursor.IsChecked = MouseSettings.FollowCursor;
            txtMinInterval.Text = Settings.RandomMinInterval.ToString();
            txtMaxInterval.Text = Settings.RandomMaxInterval.ToString();
            
            // Companion settings
            chkEnableCompanion.IsChecked = GremlinSettings.CompanionEnabled;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Dance settings
            GremlinSettings.AutoDanceEnabled = chkAutoDance.IsChecked ?? true;
            GremlinSettings.AutoStopEnabled = chkAutoStop.IsChecked ?? true;
            GremlinSettings.MusicNotesEnabled = chkMusicNotes.IsChecked ?? true;
            GremlinSettings.DynamicChanceEnabled = chkDynamicChance.IsChecked ?? false;
            GremlinSettings.DanceChancePercent = (int)sliderDanceChance.Value;
            GremlinSettings.DanceChanceIncrement = (int)sliderDanceIncrement.Value;
            GremlinSettings.StopChancePercent = (int)sliderStopChance.Value;
            GremlinSettings.StopChanceIncrement = (int)sliderStopIncrement.Value;
            GremlinSettings.DanceCheckIntervalSeconds = (int)sliderDanceCheckInterval.Value;
            GremlinSettings.DanceCooldownSeconds = (int)sliderDanceCooldown.Value;
            GremlinSettings.UmaEasterEggEnabled = chkUmaEasterEgg.IsChecked ?? true;
            
            // Audio/music detection settings
            GremlinSettings.UseMediaSession = chkUseMediaSession.IsChecked ?? true;
            GremlinSettings.UseVolumeDetection = chkUseVolumeDetection.IsChecked ?? true;
            GremlinSettings.AudioThresholdPercent = (int)sliderAudioThreshold.Value;
            GremlinSettings.AudioSensitivityMultiplier = (float)sliderAudioSensitivity.Value;
            GremlinSettings.SoundVolume = (int)sliderVolume.Value;
            MediaManager.SetVolume(GremlinSettings.SoundVolume / 100.0);
            
            // Behavior settings
            Settings.AllowRandomness = chkAllowRandomness.IsChecked ?? true;
            MouseSettings.FollowCursor = chkFollowCursor.IsChecked ?? true;
            if (int.TryParse(txtMinInterval.Text, out int minInterval))
                Settings.RandomMinInterval = minInterval;
            if (int.TryParse(txtMaxInterval.Text, out int maxInterval))
                Settings.RandomMaxInterval = maxInterval;
            
            // Companion settings
            GremlinSettings.CompanionEnabled = chkEnableCompanion.IsChecked ?? false;
            
            // Save to file
            GremlinSettings.Save();
            
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    /// <summary>
    /// New settings for extended features
    /// </summary>
    public static class GremlinSettings
    {
        private static string SettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DesktopGremlin", "settings.ini");

        // Dance settings
        public static bool AutoDanceEnabled { get; set; } = true;
        public static bool AutoStopEnabled { get; set; } = true;
        public static bool MusicNotesEnabled { get; set; } = true;
        public static bool DynamicChanceEnabled { get; set; } = false;
        public static int DanceChancePercent { get; set; } = 30;
        public static int DanceChanceIncrement { get; set; } = 2;
        public static int StopChancePercent { get; set; } = 10;
        public static int StopChanceIncrement { get; set; } = 1;
        public static int DanceCheckIntervalSeconds { get; set; } = 6;
        public static int DanceCooldownSeconds { get; set; } = 15;
        
        // Easter egg setting
        public static bool UmaEasterEggEnabled { get; set; } = true;
        
        // Dynamic chance tracking (not saved, runtime only)
        public static int CurrentDanceChanceBonus { get; set; } = 0;
        public static int CurrentStopChanceBonus { get; set; } = 0;
        
        public static int EffectiveDanceChance => DynamicChanceEnabled 
            ? Math.Min(100, DanceChancePercent + CurrentDanceChanceBonus) 
            : DanceChancePercent;
        public static int EffectiveStopChance => DynamicChanceEnabled 
            ? Math.Min(100, StopChancePercent + CurrentStopChanceBonus) 
            : StopChancePercent;
        
        // Music detection settings
        public static bool UseMediaSession { get; set; } = true;
        public static bool UseVolumeDetection { get; set; } = true;
        public static int AudioThresholdPercent { get; set; } = 5; // Default 5% instead of 2%
        public static float AudioSensitivityMultiplier { get; set; } = 1f; // Multiplier for sensitivity (can be <1 or >1)
        
        // Sound volume settings
        public static int SoundVolume { get; set; } = 100;
        
        // Calculated properties
        public static double Volume => SoundVolume / 100.0;
        public static float AudioThreshold => AudioThresholdPercent / 100.0f;
        public static float AudioSensitivity => AudioSensitivityMultiplier;
        
        // Companion settings
        public static bool CompanionEnabled { get; set; } = false;

        public static void Load()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return;

                foreach (var line in File.ReadAllLines(SettingsPath))
                {
                    if (string.IsNullOrWhiteSpace(line) || !line.Contains("=")) continue;
                    
                    var parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length != 2) continue;

                    string key = parts[0].Trim();
                    string value = parts[1].Trim();

                    switch (key)
                    {
                        case "AutoDanceEnabled":
                            if (bool.TryParse(value, out bool autoDance))
                                AutoDanceEnabled = autoDance;
                            break;
                        case "AutoStopEnabled":
                            if (bool.TryParse(value, out bool autoStop))
                                AutoStopEnabled = autoStop;
                            break;
                        case "MusicNotesEnabled":
                            if (bool.TryParse(value, out bool notes))
                                MusicNotesEnabled = notes;
                            break;
                        case "DynamicChanceEnabled":
                            if (bool.TryParse(value, out bool dynamic))
                                DynamicChanceEnabled = dynamic;
                            break;
                        case "DanceChancePercent":
                            if (int.TryParse(value, out int danceChance))
                                DanceChancePercent = danceChance;
                            break;
                        case "DanceChanceIncrement":
                            if (int.TryParse(value, out int danceInc))
                                DanceChanceIncrement = danceInc;
                            break;
                        case "StopChancePercent":
                            if (int.TryParse(value, out int stopChance))
                                StopChancePercent = stopChance;
                            break;
                        case "StopChanceIncrement":
                            if (int.TryParse(value, out int stopInc))
                                StopChanceIncrement = stopInc;
                            break;
                        case "DanceCheckIntervalSeconds":
                            if (int.TryParse(value, out int checkInterval))
                                DanceCheckIntervalSeconds = checkInterval;
                            break;
                        case "DanceCooldownSeconds":
                            if (int.TryParse(value, out int cooldown))
                                DanceCooldownSeconds = cooldown;
                            break;
                        case "UmaEasterEggEnabled":
                            if (bool.TryParse(value, out bool easterEgg))
                                UmaEasterEggEnabled = easterEgg;
                            break;
                        case "UseMediaSession":
                            if (bool.TryParse(value, out bool useMedia))
                                UseMediaSession = useMedia;
                            break;
                        case "UseVolumeDetection":
                            if (bool.TryParse(value, out bool useVolume))
                                UseVolumeDetection = useVolume;
                            break;
                        case "SoundVolume":
                            if (int.TryParse(value, out int volume))
                                SoundVolume = volume;
                            break;
                        case "AudioThresholdPercent":
                            if (int.TryParse(value, out int threshold))
                                AudioThresholdPercent = threshold;
                            break;
                        case "AudioSensitivityMultiplier":
                            if (float.TryParse(value, out float sensitivity))
                                AudioSensitivityMultiplier = sensitivity;
                            break;
                        case "CompanionEnabled":
                            if (bool.TryParse(value, out bool companion))
                                CompanionEnabled = companion;
                            break;
                    }
                }
            }
            catch
            {
                // Use defaults if file can't be read
            }
        }

        public static void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var lines = new[]
                {
                    $"AutoDanceEnabled={AutoDanceEnabled}",
                    $"AutoStopEnabled={AutoStopEnabled}",
                    $"MusicNotesEnabled={MusicNotesEnabled}",
                    $"DynamicChanceEnabled={DynamicChanceEnabled}",
                    $"DanceChancePercent={DanceChancePercent}",
                    $"DanceChanceIncrement={DanceChanceIncrement}",
                    $"StopChancePercent={StopChancePercent}",
                    $"StopChanceIncrement={StopChanceIncrement}",
                    $"DanceCheckIntervalSeconds={DanceCheckIntervalSeconds}",
                    $"DanceCooldownSeconds={DanceCooldownSeconds}",
                    $"UmaEasterEggEnabled={UmaEasterEggEnabled}",
                    $"UseMediaSession={UseMediaSession}",
                    $"UseVolumeDetection={UseVolumeDetection}",
                    $"SoundVolume={SoundVolume}",
                    $"AudioThresholdPercent={AudioThresholdPercent}",
                    $"AudioSensitivityMultiplier={AudioSensitivityMultiplier}",
                    $"CompanionEnabled={CompanionEnabled}"
                };

                File.WriteAllLines(SettingsPath, lines);
            }
            catch
            {
                // Silently fail if can't save
            }
        }
    }
}
