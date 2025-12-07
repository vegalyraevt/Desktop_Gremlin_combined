using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Desktop_Gremlin
{
    /// <summary>
    /// Displays animated music notes near the character while dancing
    /// </summary>
    public class MusicNoteOverlay : Window
    {
        private static MusicNoteOverlay _instance;
        private Image _noteImage;
        private BitmapImage _spriteSheet;
        private DispatcherTimer _animationTimer;
        private int _currentFrame = 0;
        private const int FRAME_COUNT = 123;
        private const int FRAME_WIDTH = 300;
        private const int FRAME_HEIGHT = 300;
        private const int SPRITE_COLUMN = 5;
        private Window _targetWindow;
        private Random _rng = new Random();

        public static bool IsShowing { get; private set; } = false;

        private MusicNoteOverlay(Window targetWindow)
        {
            _targetWindow = targetWindow;
            
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
            Width = 150;  // Smaller than the full frame
            Height = 150;
            
            _noteImage = new Image
            {
                Width = 150,
                Height = 150,
                Stretch = Stretch.Uniform,
                RenderTransformOrigin = new Point(0.5, 0.5)
            };
            
            Content = _noteImage;
            
            // Load sprite sheet
            LoadSpriteSheet();
            
            // Setup animation timer
            _animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) }; // ~30fps
            _animationTimer.Tick += AnimationTick;
        }

        private void LoadSpriteSheet()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SpriteSheet", "System", "music_note.png");
                if (File.Exists(path))
                {
                    _spriteSheet = new BitmapImage();
                    _spriteSheet.BeginInit();
                    _spriteSheet.UriSource = new Uri(path);
                    _spriteSheet.CacheOption = BitmapCacheOption.OnLoad;
                    _spriteSheet.EndInit();
                    _spriteSheet.Freeze();
                }
            }
            catch
            {
                // Silently fail - music notes are optional
            }
        }

        private void AnimationTick(object sender, EventArgs e)
        {
            if (_spriteSheet == null) return;

            // Calculate frame position in sprite sheet
            int x = (_currentFrame % SPRITE_COLUMN) * FRAME_WIDTH;
            int y = (_currentFrame / SPRITE_COLUMN) * FRAME_HEIGHT;

            if (x + FRAME_WIDTH <= _spriteSheet.PixelWidth && y + FRAME_HEIGHT <= _spriteSheet.PixelHeight)
            {
                _noteImage.Source = new CroppedBitmap(_spriteSheet, new Int32Rect(x, y, FRAME_WIDTH, FRAME_HEIGHT));
            }

            _currentFrame = (_currentFrame + 1) % FRAME_COUNT;

            // Update position to follow target window with some offset
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (_targetWindow == null) return;
            
            // Position the notes to the right and at character level (moved down and closer)
            Left = _targetWindow.Left + _targetWindow.Width - 60;  // Closer to character
            Top = _targetWindow.Top + 30;  // Lower, more at character level
        }

        public static void Show(Window targetWindow)
        {
            if (_instance == null)
            {
                _instance = new MusicNoteOverlay(targetWindow);
            }
            
            if (_instance._spriteSheet == null) return; // No sprite sheet loaded
            
            _instance._targetWindow = targetWindow;
            _instance._currentFrame = 0;
            _instance.UpdatePosition();
            _instance.Show();
            _instance._animationTimer.Start();
            IsShowing = true;
            
            DebugOverlay.Log("NOTES", "Music notes started");
        }

        public static new void Hide()
        {
            if (_instance != null && IsShowing)
            {
                _instance._animationTimer.Stop();
                ((Window)_instance).Hide();
                IsShowing = false;
                
                DebugOverlay.Log("NOTES", "Music notes stopped");
            }
        }

        public static void UpdateTargetPosition(Window targetWindow)
        {
            if (_instance != null && IsShowing)
            {
                _instance._targetWindow = targetWindow;
                _instance.UpdatePosition();
            }
        }
    }
}
