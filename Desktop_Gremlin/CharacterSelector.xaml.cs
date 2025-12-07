using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Desktop_Gremlin
{
    public class CharacterListItem : INotifyPropertyChanged
    {
        private bool _isCurrent;
        
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Initial => string.IsNullOrEmpty(DisplayName) ? "?" : DisplayName[0].ToString();
        
        public bool IsCurrent
        {
            get => _isCurrent;
            set
            {
                _isCurrent = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCurrent)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public partial class CharacterSelector : Window
    {
        public string SelectedCharacter { get; private set; }
        public bool CharacterSelected { get; private set; } = false;
        
        private List<CharacterListItem> _characters;

        public CharacterSelector()
        {
            InitializeComponent();
            LoadCharacters();
        }

        private void LoadCharacters()
        {
            try
            {
                // Initialize character manager if not already done
                CharacterManager.Initialize();
                
                var availableCharacters = CharacterManager.GetAvailableCharacters();
                var currentCharacter = CharacterManager.GetCurrentCharacter();
                
                _characters = availableCharacters.Select(name => 
                {
                    var characterInfo = CharacterManager.GetCharacterInfo(name);
                    return new CharacterListItem
                    {
                        Name = name,
                        DisplayName = GetDisplayName(name),
                        Description = GetCharacterDescription(name, characterInfo),
                        IsCurrent = name == currentCharacter
                    };
                }).ToList();

                CharacterListBox.ItemsSource = _characters;
                
                // Select current character
                var currentItem = _characters.FirstOrDefault(c => c.IsCurrent);
                if (currentItem != null)
                {
                    CharacterListBox.SelectedItem = currentItem;
                }
                else if (_characters.Any())
                {
                    CharacterListBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load characters: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private string GetDisplayName(string characterName)
        {
            // Convert internal names to friendly display names
            switch (characterName)
            {
                case "MamboFarmer": return "Mambo Farmer";
                case "Agnes_Alt": return "Agnes (Alternative)";
                case "Exu": return "Exusiai";
                case "Exu2": return "Exusiai (Alternative)";
                case "GoldShip": return "Gold Ship";
                case "Cafe": return "Manhattan Cafe";
                default: return characterName;
            }
        }

        private string GetCharacterDescription(string name, CharacterInfo info)
        {
            if (info == null) return "No information available";

            var animations = new List<string>();
            
            // Check what animations this character supports
            if (info.FrameCounts.Idle > 0) animations.Add("Idle");
            if (info.FrameCounts.Left > 0 || info.FrameCounts.Right > 0) animations.Add("Running");
            if (info.FrameCounts.WalkL > 0 || info.FrameCounts.WalkR > 0) animations.Add("Walking");
            if (info.FrameCounts.Emote1 > 0) animations.Add("Emotes");
            if (info.FrameCounts.Grab > 0) animations.Add("Interactive");
            if (info.FrameCounts.Dance > 0) animations.Add("Dancing");
            
            // Check if character has companion support
            if (CharacterFeatureManager.SupportsCompanion(name)) animations.Add("Companion");
            
            var animationText = animations.Any() ? string.Join(", ", animations) : "Basic";
            return string.Format("{0}x{1} • {2}", info.FrameWidth, info.FrameHeight, animationText);
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            SelectCurrentCharacter();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CharacterSelected = false;
            Close();
        }

        private void CharacterListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectCurrentCharacter();
        }

        private void SelectCurrentCharacter()
        {
            var selectedItem = CharacterListBox.SelectedItem as CharacterListItem;
            if (selectedItem != null)
            {
                SelectedCharacter = selectedItem.Name;
                CharacterSelected = true;
                Close();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            
            if (e.Key == Key.Enter)
            {
                SelectCurrentCharacter();
            }
            else if (e.Key == Key.Escape)
            {
                CancelButton_Click(null, null);
            }
        }
    }
}