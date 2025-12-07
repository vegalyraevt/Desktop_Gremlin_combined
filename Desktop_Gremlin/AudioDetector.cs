using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Desktop_Gremlin
{
    /// <summary>
    /// Detects if audio/music is currently playing on the system
    /// </summary>
    public static class AudioDetector
    {
        // COM interfaces for Windows Core Audio API
        [ComImport]
        [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumerator { }

        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            int NotImpl1();
            [PreserveSig]
            int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppDevice);
        }

        [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            [PreserveSig]
            int Activate([MarshalAs(UnmanagedType.LPStruct)] Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
        }

        [Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioMeterInformation
        {
            [PreserveSig]
            int GetPeakValue(out float pfPeak);
        }

        private static readonly Guid IID_IAudioMeterInformation = new Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064");
        
        private static IMMDevice _device;
        private static IAudioMeterInformation _meter;
        private static bool _initialized = false;
        private static DateTime _lastInitAttempt = DateTime.MinValue;
        
        // For sustained audio detection
        private static DateTime _audioStartTime = DateTime.MinValue;
        private static float _accumulatedLevel = 0f;
        private static int _sampleCount = 0;
        private static DateTime _lastSampleTime = DateTime.MinValue;
        
        // Media session detection
        private static bool _mediaSessionInitialized = false;
        private static bool _mediaSessionAvailable = false;
        private static bool _lastMediaSessionResult = false;
        private static DateTime _lastMediaSessionCheck = DateTime.MinValue;
        private static string _lastMediaSource = "";
        private static string _lastSessionError = "";
        private static int _lastSessionCount = 0;
        private static string _allActiveSessions = "";
        
        // Media info from Windows Media Session
        private static string _currentMediaTitle = "";
        private static string _currentMediaArtist = "";
        private static string _currentMediaApp = "";
        
        // Track which detection method was used
        public static string LastDetectionMethod { get; private set; } = "None";
        public static string LastMediaSource => _lastMediaSource;
        public static string LastSessionError => _lastSessionError;
        public static int LastSessionCount => _lastSessionCount;
        public static string AllActiveSessions => _allActiveSessions;
        public static float LastRawLevel { get; private set; } = 0f;
        public static bool MediaSessionAvailable => _mediaSessionAvailable;
        
        // Media info properties
        public static string CurrentMediaTitle => _currentMediaTitle;
        public static string CurrentMediaArtist => _currentMediaArtist;
        public static string CurrentMediaApp => _currentMediaApp;
        public static string CurrentMediaInfo => !string.IsNullOrEmpty(_currentMediaTitle) 
            ? (!string.IsNullOrEmpty(_currentMediaArtist) ? $"{_currentMediaArtist} - {_currentMediaTitle}" : _currentMediaTitle)
            : "";
        
        // Uma Musume song detection
        public static bool IsUmaSongPlaying { get; private set; } = false;
        public static string DetectedUmaSong { get; private set; } = "";
        
        // Music streaming apps (not video sites like YouTube where guides could trigger false positives)
        private static readonly string[] MusicOnlyApps = new[]
        {
            "spotify", "musicbee", "foobar2000", "winamp", "aimp", "itunes",
            "groove", "amazon music", "deezer", "tidal", "apple music",
            "soundcloud", "pandora", "youtube music"
        };
        
        // 100% unique Uma terms - safe to match anywhere
        private static readonly string[][] UniqueUmaTerms = new string[][]
        {
            new[] { "Umapyoi", "うまぴょい" },  // 100% unique to Uma
        };
        
        // Terms that need to come from music apps only (could be in YouTube guides)
        private static readonly string[][] MusicAppOnlyTerms = new string[][]
        {
            new[] { "Bakushin", "バクシン" },  // Could be in guide videos
            new[] { "Tracen", "トレセン" },  // Tracen Academy references in guides
            new[] { "ウマ娘" },  // Japanese "Uma Musume" - common in guides
        };
        
        // Generic terms that need Uma/character context to match
        private static readonly (string[] terms, string[] requiredContext)[] ContextRequiredTerms = new (string[], string[])[]
        {
            (new[] { "transforming" }, new[] { "oguri", "オグリ", "uma", "umamusume", "ウマ娘" }),
            (new[] { "Special Record" }, new[] { "uma", "umamusume", "ウマ娘" }),
        };
        
        // Full song titles - unique enough to match anywhere
        private static readonly string[][] FullSongTitles = new string[][]
        {
            new[] { "Bakushin Bakushin", "バクシンバクシン" },  // Bakushin's signature song
            new[] { "GIRLS LEGEND U", "GIRLS' LEGEND U" },
            new[] { "Make debut" },
            new[] { "Yume wo Kakeru", "ユメヲカケル" },
            new[] { "Winning the Soul" },
            new[] { "NEXT FRONTIER" },
            new[] { "Ms VICTORIA", "Ms. VICTORIA" },
            new[] { "Gaze on Me" },
            new[] { "UNLIMITED IMPACT" },
            new[] { "We are DREAMERS" },
            new[] { "BLOW my GALE" },
            new[] { "Ambitious World" },
            new[] { "DRAMATIC JOURNEY" },
            new[] { "Hashire Uma Musume", "走れウマ娘" },
            new[] { "Komorebi no Yell", "木漏れ日のエール" },
            new[] { "Sasayaka na Inori", "ささやかな祈り" },
            new[] { "Hat on your Head" },
            new[] { "Ready Steady Derby" },
            new[] { "BoC-izm", "BoC izm" },
            new[] { "L Arc de gloire", "L'Arc de gloire" },
            new[] { "Grow-up Shine", "Grow up Shine" },
            new[] { "Honnou Speed", "本能スピード" },
            new[] { "Fanfare for Future" },
        };
        
        /// <summary>
        /// Check if current media app is a music streaming service (not YouTube/browser)
        /// </summary>
        private static bool IsMusicStreamingApp()
        {
            if (string.IsNullOrEmpty(_currentMediaApp)) return false;
            string appLower = _currentMediaApp.ToLowerInvariant();
            
            foreach (var app in MusicOnlyApps)
            {
                if (appLower.Contains(app)) return true;
            }
            return false;
        }
        
        /// <summary>
        /// Normalize text for fuzzy matching - removes punctuation and converts to lowercase
        /// </summary>
        private static string NormalizeForMatching(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            
            // Remove common punctuation and special characters, convert to lowercase
            var sb = new System.Text.StringBuilder();
            foreach (char c in text.ToLowerInvariant())
            {
                // Keep letters, numbers, and Japanese characters
                if (char.IsLetterOrDigit(c) || c > 0x3000) // Japanese chars are > 0x3000
                {
                    sb.Append(c);
                }
                else if (char.IsWhiteSpace(c))
                {
                    sb.Append(' ');
                }
            }
            return sb.ToString();
        }
        
        /// <summary>
        /// Check if the current media title matches any Uma Musume song
        /// </summary>
        public static bool CheckForUmaSong()
        {
            if (string.IsNullOrEmpty(_currentMediaTitle))
            {
                IsUmaSongPlaying = false;
                DetectedUmaSong = "";
                return false;
            }
            
            string normalizedTitle = NormalizeForMatching(_currentMediaTitle);
            string normalizedArtist = NormalizeForMatching(_currentMediaArtist);
            string combined = normalizedTitle + " " + normalizedArtist;
            bool isMusicApp = IsMusicStreamingApp();
            
            // Debug log
            DebugOverlay.Log("UMA-CHK", $"App:{_currentMediaApp} Music:{isMusicApp}");
            
            // 1. Check 100% unique terms (match anywhere)
            foreach (var variants in UniqueUmaTerms)
            {
                foreach (var variant in variants)
                {
                    if (combined.Contains(NormalizeForMatching(variant)))
                    {
                        IsUmaSongPlaying = true;
                        DetectedUmaSong = variants[0];
                        DebugOverlay.Log("UMA-CHK", $"UNIQUE MATCH: '{variant}'");
                        return true;
                    }
                }
            }
            
            // 2. Check terms that only work from music apps
            if (isMusicApp)
            {
                foreach (var variants in MusicAppOnlyTerms)
                {
                    foreach (var variant in variants)
                    {
                        if (combined.Contains(NormalizeForMatching(variant)))
                        {
                            IsUmaSongPlaying = true;
                            DetectedUmaSong = variants[0];
                            DebugOverlay.Log("UMA-CHK", $"MUSIC APP MATCH: '{variant}'");
                            return true;
                        }
                    }
                }
            }
            
            // 3. Check terms that need Uma/character context
            foreach (var (terms, requiredContext) in ContextRequiredTerms)
            {
                bool hasContext = false;
                foreach (var ctx in requiredContext)
                {
                    if (combined.Contains(NormalizeForMatching(ctx)))
                    {
                        hasContext = true;
                        break;
                    }
                }
                
                if (hasContext)
                {
                    foreach (var term in terms)
                    {
                        if (combined.Contains(NormalizeForMatching(term)))
                        {
                            IsUmaSongPlaying = true;
                            DetectedUmaSong = terms[0];
                            DebugOverlay.Log("UMA-CHK", $"CONTEXT MATCH: '{term}' with Uma context");
                            return true;
                        }
                    }
                }
            }
            
            // 4. Check full song titles (unique enough to match anywhere)
            foreach (var variants in FullSongTitles)
            {
                foreach (var variant in variants)
                {
                    if (combined.Contains(NormalizeForMatching(variant)))
                    {
                        IsUmaSongPlaying = true;
                        DetectedUmaSong = variants[0];
                        DebugOverlay.Log("UMA-CHK", $"TITLE MATCH: '{variant}'");
                        return true;
                    }
                }
            }
            
            IsUmaSongPlaying = false;
            DetectedUmaSong = "";
            return false;
        }

        /// <summary>
        /// Initialize the audio meter. Call once at startup.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            
            // Don't spam init attempts if it fails
            if ((DateTime.Now - _lastInitAttempt).TotalSeconds < 30) return;
            _lastInitAttempt = DateTime.Now;

            try
            {
                var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
                // eRender = 0 (speakers), eConsole = 0
                int hr = enumerator.GetDefaultAudioEndpoint(0, 0, out _device);
                if (hr != 0 || _device == null) return;

                object meterObj;
                hr = _device.Activate(IID_IAudioMeterInformation, 1, IntPtr.Zero, out meterObj);
                if (hr != 0 || meterObj == null) return;

                _meter = (IAudioMeterInformation)meterObj;
                _initialized = true;
            }
            catch
            {
                // Silently fail - audio detection is optional
                _initialized = false;
            }
        }

        /// <summary>
        /// Check if audio is currently playing (above threshold)
        /// </summary>
        /// <param name="threshold">Volume threshold (0.0 to 1.0), default 0.01 (1%)</param>
        /// <returns>True if audio is playing above threshold</returns>
        public static bool IsAudioPlaying(float threshold = 0.01f)
        {
            float peak = GetAudioLevel() * GremlinSettings.AudioSensitivity;
            return peak > threshold;
        }
        
        /// <summary>
        /// Check if sustained audio is playing (for music detection)
        /// Audio must be above threshold for at least the specified duration
        /// </summary>
        /// <param name="threshold">Volume threshold (0.0 to 1.0)</param>
        /// <param name="sustainedSeconds">How long audio must play to be considered "sustained"</param>
        /// <returns>True if sustained audio detected</returns>
        public static bool IsSustainedAudioPlaying(float threshold = 0.02f, float sustainedSeconds = 1.0f)
        {
            float peak = GetAudioLevel() * GremlinSettings.AudioSensitivity;
            
            if (peak > threshold)
            {
                if (_audioStartTime == DateTime.MinValue)
                {
                    _audioStartTime = DateTime.Now;
                }
                
                double duration = (DateTime.Now - _audioStartTime).TotalSeconds;
                return duration >= sustainedSeconds;
            }
            else
            {
                // Reset when audio drops below threshold
                _audioStartTime = DateTime.MinValue;
                return false;
            }
        }
        
        /// <summary>
        /// Get average audio level over recent samples (smoother detection)
        /// </summary>
        public static float GetAverageAudioLevel(int sampleWindow = 5)
        {
            float current = GetAudioLevel();
            
            // Reset if too much time has passed
            if ((DateTime.Now - _lastSampleTime).TotalSeconds > 1)
            {
                _accumulatedLevel = 0f;
                _sampleCount = 0;
            }
            
            _accumulatedLevel += current;
            _sampleCount++;
            _lastSampleTime = DateTime.Now;
            
            // Keep a rolling window
            if (_sampleCount > sampleWindow)
            {
                _accumulatedLevel = _accumulatedLevel * sampleWindow / _sampleCount;
                _sampleCount = sampleWindow;
            }
            
            return _sampleCount > 0 ? _accumulatedLevel / _sampleCount : 0f;
        }

        /// <summary>
        /// Get the current audio peak level (0.0 to 1.0)
        /// </summary>
        public static float GetAudioLevel()
        {
            if (!_initialized)
            {
                Initialize();
                if (!_initialized) return 0f;
            }

            try
            {
                float peak;
                int hr = _meter.GetPeakValue(out peak);
                if (hr == 0)
                {
                    LastRawLevel = peak;
                    return peak;
                }
                return 0f;
            }
            catch
            {
                return 0f;
            }
        }
        
        /// <summary>
        /// Check if media is currently playing using Windows Media Session
        /// This detects actual media apps (Spotify, browsers, etc.) not just any audio
        /// </summary>
        public static bool IsMediaPlaying()
        {
            // Cache result for 500ms to avoid hammering the API
            if ((DateTime.Now - _lastMediaSessionCheck).TotalMilliseconds < 500)
            {
                return _lastMediaSessionResult;
            }
            _lastMediaSessionCheck = DateTime.Now;
            
            try
            {
                _lastMediaSessionResult = CheckMediaSessionPlaying();
                return _lastMediaSessionResult;
            }
            catch
            {
                _lastMediaSessionResult = false;
                _mediaSessionAvailable = false;
                return false;
            }
        }
        
        public static bool IsMediaSessionAvailable => _mediaSessionAvailable;
        
        /// <summary>
        /// Initialize media session detection - call this early to populate availability status
        /// </summary>
        public static void InitializeMediaSession()
        {
            if (_mediaSessionInitialized) return;
            
            _mediaSessionInitialized = true;
            try
            {
                // Environment.OSVersion is unreliable on Win10/11 due to compatibility shims
                // Use Registry to get actual Windows version
                int buildNumber = 0;
                try
                {
                    using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                    {
                        if (key != null)
                        {
                            // Try CurrentBuildNumber first (more reliable)
                            var buildStr = key.GetValue("CurrentBuildNumber") as string;
                            if (!string.IsNullOrEmpty(buildStr) && int.TryParse(buildStr, out int build))
                            {
                                buildNumber = build;
                            }
                        }
                    }
                }
                catch { }
                
                // Windows 10 1809 is build 17763
                // If we couldn't get build from registry, assume we're on a modern Windows and try anyway
                if (buildNumber > 0 && buildNumber < 17763)
                {
                    _mediaSessionAvailable = false;
                    _lastSessionError = $"Build {buildNumber} < 17763 (Win10 1809)";
                    return;
                }
                
                // Assume available - actual COM calls will fail gracefully if not supported
                _mediaSessionAvailable = true;
                _lastSessionError = buildNumber > 0 ? $"Build {buildNumber} OK" : "Assuming compatible";
            }
            catch (Exception ex)
            {
                _mediaSessionAvailable = false;
                _lastSessionError = ex.Message;
            }
        }
        
        private static bool CheckMediaSessionPlaying()
        {
            // Ensure initialized
            if (!_mediaSessionInitialized)
            {
                InitializeMediaSession();
            }
            
            if (!_mediaSessionAvailable) return false;
            
            try
            {
                // Use WinRT interop to access media session
                // We'll use a simpler approach: check for playing audio sessions via audio endpoint
                return CheckAudioSessionsPlaying();
            }
            catch
            {
                return false;
            }
        }
        
        // Audio Session interfaces for checking individual app playback states
        // IAudioSessionControl - correct method order per Windows SDK
        [Guid("F4B1A599-7266-4319-A8CA-E70ACB11E8CD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioSessionControl
        {
            [PreserveSig]
            int GetState(out int pRetVal);
            [PreserveSig]
            int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
            [PreserveSig]
            int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
            [PreserveSig]
            int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
            [PreserveSig]
            int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
            [PreserveSig]
            int GetGroupingParam(out Guid pRetVal);
            [PreserveSig]
            int SetGroupingParam([MarshalAs(UnmanagedType.LPStruct)] Guid Override, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
            [PreserveSig]
            int RegisterAudioSessionNotification(IntPtr NewNotifications);
            [PreserveSig]
            int UnregisterAudioSessionNotification(IntPtr NewNotifications);
        }

        // IAudioSessionControl2 extends IAudioSessionControl
        [Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioSessionControl2
        {
            // IAudioSessionControl methods (must be first)
            [PreserveSig]
            int GetState(out int pRetVal);
            [PreserveSig]
            int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
            [PreserveSig]
            int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
            [PreserveSig]
            int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
            [PreserveSig]
            int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
            [PreserveSig]
            int GetGroupingParam(out Guid pRetVal);
            [PreserveSig]
            int SetGroupingParam([MarshalAs(UnmanagedType.LPStruct)] Guid Override, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);
            [PreserveSig]
            int RegisterAudioSessionNotification(IntPtr NewNotifications);
            [PreserveSig]
            int UnregisterAudioSessionNotification(IntPtr NewNotifications);
            
            // IAudioSessionControl2 methods
            [PreserveSig]
            int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
            [PreserveSig]
            int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);
            [PreserveSig]
            int GetProcessId(out uint pRetVal);
            [PreserveSig]
            int IsSystemSoundsSession();
            [PreserveSig]
            int SetDuckingPreference(bool optOut);
        }
        
        [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioSessionManager2
        {
            // IAudioSessionManager methods
            [PreserveSig]
            int GetAudioSessionControl(IntPtr AudioSessionGuid, int StreamFlags, out IAudioSessionControl SessionControl);
            [PreserveSig]
            int GetSimpleAudioVolume(IntPtr AudioSessionGuid, int StreamFlags, out IntPtr AudioVolume);
            
            // IAudioSessionManager2 methods
            [PreserveSig]
            int GetSessionEnumerator(out IAudioSessionEnumerator SessionEnum);
            [PreserveSig]
            int RegisterSessionNotification(IntPtr SessionNotification);
            [PreserveSig]
            int UnregisterSessionNotification(IntPtr SessionNotification);
            [PreserveSig]
            int RegisterDuckNotification([MarshalAs(UnmanagedType.LPWStr)] string sessionID, IntPtr duckNotification);
            [PreserveSig]
            int UnregisterDuckNotification(IntPtr duckNotification);
        }
        
        [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioSessionEnumerator
        {
            [PreserveSig]
            int GetCount(out int SessionCount);
            [PreserveSig]
            int GetSession(int SessionCount, out IAudioSessionControl2 Session);
        }
        
        private static readonly Guid IID_IAudioSessionManager2 = new Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F");
        
        // AudioSessionState enum values
        private const int AudioSessionStateInactive = 0;
        private const int AudioSessionStateActive = 1;
        private const int AudioSessionStateExpired = 2;
        
        private static bool CheckAudioSessionsPlaying()
        {
            _lastSessionError = "";
            _lastSessionCount = 0;
            _allActiveSessions = "";
            
            try
            {
                if (_device == null)
                {
                    Initialize();
                    if (_device == null)
                    {
                        _lastSessionError = "No audio device";
                        return false;
                    }
                }
                
                object sessionManagerObj;
                int hr = _device.Activate(IID_IAudioSessionManager2, 1, IntPtr.Zero, out sessionManagerObj);
                if (hr != 0 || sessionManagerObj == null)
                {
                    _lastSessionError = $"SessionManager failed: 0x{hr:X8}";
                    return false;
                }
                
                var sessionManager = (IAudioSessionManager2)sessionManagerObj;
                IAudioSessionEnumerator sessionEnumerator;
                hr = sessionManager.GetSessionEnumerator(out sessionEnumerator);
                if (hr != 0 || sessionEnumerator == null) 
                {
                    _lastSessionError = $"Enumerator failed: 0x{hr:X8}";
                    Marshal.ReleaseComObject(sessionManager);
                    return false;
                }
                
                int sessionCount;
                sessionEnumerator.GetCount(out sessionCount);
                _lastSessionCount = sessionCount;
                
                bool foundPlaying = false;
                var activeNames = new System.Collections.Generic.List<string>();
                
                for (int i = 0; i < sessionCount; i++)
                {
                    IAudioSessionControl2 session;
                    hr = sessionEnumerator.GetSession(i, out session);
                    if (hr != 0 || session == null) continue;
                    
                    try
                    {
                        // Check if it's system sounds (skip those)
                        int isSystem = session.IsSystemSoundsSession();
                        if (isSystem == 0) // S_OK (0) means it IS system sounds
                        {
                            continue;
                        }
                        
                        // Check session state
                        int state;
                        if (session.GetState(out state) == 0)
                        {
                            // Get process info regardless of state for debug
                            uint processId = 0;
                            string procName = "Unknown";
                            
                            if (session.GetProcessId(out processId) == 0 && processId > 0)
                            {
                                try
                                {
                                    var process = System.Diagnostics.Process.GetProcessById((int)processId);
                                    procName = process.ProcessName;
                                }
                                catch { procName = $"PID:{processId}"; }
                            }
                            
                            // Track active sessions
                            if (state == AudioSessionStateActive)
                            {
                                activeNames.Add(procName);
                                
                                // Check if this is a media app
                                if (IsMediaApp(procName.ToLower()))
                                {
                                    _lastMediaSource = procName;
                                    foundPlaying = true;
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (session != null)
                            Marshal.ReleaseComObject(session);
                    }
                }
                
                _allActiveSessions = activeNames.Count > 0 
                    ? string.Join(", ", activeNames) 
                    : "None";
                
                Marshal.ReleaseComObject(sessionEnumerator);
                Marshal.ReleaseComObject(sessionManager);
                
                if (!foundPlaying) _lastMediaSource = "";
                return foundPlaying;
            }
            catch (Exception ex)
            {
                _lastSessionError = ex.Message;
                System.Diagnostics.Debug.WriteLine($"CheckAudioSessionsPlaying error: {ex.Message}");
                return false;
            }
        }
        
        private static bool IsMediaApp(string processName)
        {
            // Common media applications
            string[] mediaApps = new[]
            {
                "spotify", "musicbee", "foobar2000", "winamp", "aimp", "itunes",
                "vlc", "wmplayer", "groove", "amazon music", "deezer", "tidal",
                "chrome", "firefox", "msedge", "opera", "brave", // Browsers (YouTube, etc.)
                "discord", // Discord can play audio
                "mpv", "mpc-hc", "potplayer", "kmplayer", // Video players
            };
            
            foreach (var app in mediaApps)
            {
                if (processName.Contains(app))
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// Combined detection: tries media session first, then sustained audio
        /// </summary>
        /// <param name="useMediaSession">Whether to try media session detection</param>
        /// <param name="useVolumeDetection">Whether to use volume-based detection</param>
        /// <param name="threshold">Volume threshold for audio detection</param>
        /// <returns>True if music/media is detected</returns>
        public static bool IsMusicPlaying(bool useMediaSession = true, bool useVolumeDetection = true, float threshold = 0.02f)
        {
            LastDetectionMethod = "None";
            
            // Try to update media info (async but fire-and-forget for display)
            UpdateMediaInfoAsync();
            
            // Try media session first (more accurate)
            if (useMediaSession && IsMediaPlaying())
            {
                LastDetectionMethod = "MediaSession";
                return true;
            }
            
            // Fall back to sustained volume detection
            if (useVolumeDetection)
            {
                if (IsSustainedAudioPlaying(threshold, 1.5f))
                {
                    LastDetectionMethod = "Volume";
                    return true;
                }
            }
            
            return false;
        }
        
        private static DateTime _lastMediaInfoUpdate = DateTime.MinValue;
        
        /// <summary>
        /// Updates media info (title, artist) from Windows Media Session API
        /// </summary>
        public static async void UpdateMediaInfoAsync()
        {
            // Only update every 2 seconds to avoid hammering the API
            if ((DateTime.Now - _lastMediaInfoUpdate).TotalSeconds < 2)
                return;
            _lastMediaInfoUpdate = DateTime.Now;
            
            try
            {
                // Access WinRT API
                var sessionManager = await Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                if (sessionManager == null)
                {
                    _currentMediaTitle = "";
                    _currentMediaArtist = "";
                    _currentMediaApp = "";
                    return;
                }
                
                var currentSession = sessionManager.GetCurrentSession();
                if (currentSession == null)
                {
                    _currentMediaTitle = "";
                    _currentMediaArtist = "";
                    _currentMediaApp = "";
                    return;
                }
                
                // Get app info
                _currentMediaApp = currentSession.SourceAppUserModelId ?? "";
                // Clean up app ID to just show name
                if (_currentMediaApp.Contains("!"))
                    _currentMediaApp = _currentMediaApp.Substring(0, _currentMediaApp.IndexOf("!"));
                if (_currentMediaApp.Contains("\\"))
                    _currentMediaApp = _currentMediaApp.Substring(_currentMediaApp.LastIndexOf("\\") + 1);
                if (_currentMediaApp.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    _currentMediaApp = _currentMediaApp.Substring(0, _currentMediaApp.Length - 4);
                
                // Get media properties
                var mediaProperties = await currentSession.TryGetMediaPropertiesAsync();
                if (mediaProperties != null)
                {
                    _currentMediaTitle = mediaProperties.Title ?? "";
                    _currentMediaArtist = mediaProperties.Artist ?? "";
                    
                    // If no artist but has album artist, use that
                    if (string.IsNullOrEmpty(_currentMediaArtist) && !string.IsNullOrEmpty(mediaProperties.AlbumArtist))
                    {
                        _currentMediaArtist = mediaProperties.AlbumArtist;
                    }
                }
            }
            catch
            {
                // WinRT not available or other error - leave values as-is
            }
        }
    }
}
