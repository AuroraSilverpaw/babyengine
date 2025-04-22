using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows; // Needed only if raising events requires Dispatcher

namespace BabyEngine.Models
{
    public class Achievement : INotifyPropertyChanged
    {
        private string _title = "";
        private string _description = "";
        private string _icon = "";
        private bool _isUnlocked;
        private DateTime? _unlockedDate;
        private Guid _id = Guid.NewGuid();

        public string Title { get => _title; set => SetProperty(ref _title, value); }
        public string Description { get => _description; set => SetProperty(ref _description, value); }
        public string Icon { get => _icon; set => SetProperty(ref _icon, value); }
        public bool IsUnlocked { get => _isUnlocked; set => SetProperty(ref _isUnlocked, value); }
        public DateTime? UnlockedDate { get => _unlockedDate; set => SetProperty(ref _unlockedDate, value); }
        public Guid Id { get => _id; } // ID should be read-only after creation

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    public class AchievementService : INotifyPropertyChanged
    {
        private readonly string _dataFilePath;
        private List<Achievement> _achievements = new List<Achievement>();
        
        public event PropertyChangedEventHandler? PropertyChanged;
        // Event raised when an achievement is unlocked
        public event EventHandler<Achievement>? AchievementUnlocked;
        
        // Provides a read-only view or copy of the achievements
        public IReadOnlyList<Achievement> Achievements => _achievements.AsReadOnly();
        
        public AchievementService()
        {
            // Set up data file path
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BabyEngine"
            );
            
            Directory.CreateDirectory(appDataPath);
            _dataFilePath = Path.Combine(appDataPath, "achievements.json");
            
            InitializeDefaultAchievements(); // Define defaults first
            LoadAchievements(); // Load saved state, potentially overwriting defaults if they exist
        }
        
        public void UnlockAchievement(string achievementId)
        {
            Guid idToUnlock;
            if (!Guid.TryParse(achievementId, out idToUnlock)) {
                 Console.WriteLine($"Warning: Invalid GUID format for achievement ID: {achievementId}");
                 return; 
            }

            var achievement = _achievements.FirstOrDefault(a => a.Id == idToUnlock);
            if (achievement != null && !achievement.IsUnlocked)
            {
                achievement.IsUnlocked = true;
                achievement.UnlockedDate = DateTime.Now;
                
                SaveAchievements(); // Save the updated state
                OnPropertyChanged(nameof(Achievements)); // Notify that the collection state changed (though specific item changed)
                
                 // Raise the event on the UI thread if subscribers need it
                 Application.Current?.Dispatcher.Invoke(() => {
                      AchievementUnlocked?.Invoke(this, achievement);
                 });
            }
        }
        
        // Resets all achievements to locked state
        public void ResetAchievements()
        {
            bool changed = false;
            foreach (var achievement in _achievements)
            {
                if (achievement.IsUnlocked) {
                     achievement.IsUnlocked = false;
                     achievement.UnlockedDate = null;
                     changed = true;
                }
            }
            
            if (changed) {
                 SaveAchievements();
                 OnPropertyChanged(nameof(Achievements));
                 // Maybe raise a general "AchievementsReset" event?
            }
        }
        
        // Defines the default set of achievements if none are loaded
        private void InitializeDefaultAchievements()
        {
            _achievements = new List<Achievement>
            {
                new Achievement 
                { 
                    // Id = Guid.Parse("f47ac10b-58cc-4372-a567-0e02b2c3d479"), // Set via Guid.NewGuid() or fixed in constructor if needed
                    Title = "First Chat", 
                    Description = "Had your first conversation with Mommy",
                    Icon = "💬" 
                },
                new Achievement 
                { 
                    // Id = Guid.Parse("a3b8d4e1-7b1a-4892-9f39-5f6a7b4c5d80"),
                    Title = "Consistent Little", 
                    Description = "Chat with Mommy for 3 days in a row",
                    Icon = "📅" 
                },
                new Achievement 
                { 
                    // Id = Guid.Parse("c5e9a2b1-3d7e-4a8f-b8d3-1e9a8b7c6d91"),
                    Title = "Bedtime Story", 
                    Description = "Ask Mommy for a bedtime story",
                    Icon = "📚" 
                },
                new Achievement 
                { 
                    // Id = Guid.Parse("e1f2d3c4-b5a6-7890-1234-56789abcdef0"), 
                    Title = "Good Little One", 
                    Description = "Complete 3 reminders on time",
                    Icon = "⭐" 
                },
                 new Achievement 
                { 
                    // Id = Guid.Parse("b9a8c7d6-e5f4-3210-fedc-ba9876543210"), 
                    Title = "Mood Tracker", 
                    Description = "Record your mood for 3 days",
                    Icon = "📊" 
                },
                 new Achievement 
                { 
                    // Id = Guid.Parse("1a2b3c4d-5e6f-7890-abcd-ef1234567890"), 
                    Title = "Deep Conversation", 
                    Description = "Have a long meaningful conversation with Mommy",
                    Icon = "❤️" 
                }
            };
        }
        
        private void LoadAchievements()
        {
            if (!File.Exists(_dataFilePath))
            {
                // If file doesn't exist, save the defaults we just initialized
                SaveAchievements();
                return;
            }

            try
            {
                string json = File.ReadAllText(_dataFilePath);
                var loadedAchievements = JsonSerializer.Deserialize<List<Achievement>>(json);
                
                if (loadedAchievements != null)
                {
                    // Merge loaded state with defaults: Update existing, keep defaults if not loaded
                    var defaultAchievements = _achievements; // Keep the defaults defined above
                    _achievements = new List<Achievement>();

                    foreach(var defaultAch in defaultAchievements)
                    {
                         var loadedAch = loadedAchievements.FirstOrDefault(la => la.Id == defaultAch.Id);
                         if (loadedAch != null) {
                              // Update state from loaded file
                              defaultAch.IsUnlocked = loadedAch.IsUnlocked;
                              defaultAch.UnlockedDate = loadedAch.UnlockedDate;
                               // Copy other potentially saved state if needed
                         }
                         // Add the (potentially updated) default achievement to the final list
                         _achievements.Add(defaultAch);
                    }
                }
                // If loadedAchievements is null (empty or invalid file), we keep the initialized defaults.
            }
             catch (JsonException ex)
            {
                Console.WriteLine($"Error loading achievements (invalid JSON): {ex.Message}. Using defaults.");
                 // Keep default achievements initialized earlier
                 // Optionally backup corrupt file
            }
            catch (IOException ex)
            {
                 Console.WriteLine($"Error reading achievements file: {ex.Message}. Using defaults.");
                 // Keep default achievements
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error loading achievements: {ex.Message}. Using defaults.");
                 // Keep default achievements
            }
            // Notify UI after attempting load
            OnPropertyChanged(nameof(Achievements));
        }
        
        private void SaveAchievements()
        {
            try
            {
                string json = JsonSerializer.Serialize(_achievements, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                File.WriteAllText(_dataFilePath, json);
            }
             catch (JsonException ex)
            {
                 Console.WriteLine($"Error serializing achievements: {ex.Message}");
            }
            catch (IOException ex)
            {
                 Console.WriteLine($"Error writing achievements file: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error saving achievements: {ex.Message}");
            }
        }
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 