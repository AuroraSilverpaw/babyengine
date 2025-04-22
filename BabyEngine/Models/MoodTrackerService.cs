using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace BabyEngine.Models
{
    public class MoodEntry
    {
        public string Mood { get; set; } = "";
        public string Note { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
    
    public class MoodTrackerService : INotifyPropertyChanged
    {
        private readonly string _dataFilePath;
        private List<MoodEntry> _entries = new List<MoodEntry>();
        private string _currentMood = "😊"; // Default mood
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        public List<MoodEntry> Entries => _entries;
        
        public string CurrentMood
        {
            get => _currentMood;
            set
            {
                if (_currentMood != value)
                {
                    _currentMood = value;
                    OnPropertyChanged();
                }
            }
        }
        
        // Static dictionary of available moods
        public static readonly Dictionary<string, string> MoodOptions = new Dictionary<string, string>
        {
            { "Happy", "😊" },
            { "Excited", "🤩" },
            { "Playful", "😋" },
            { "Little", "🥺" },
            { "Tired", "😴" },
            { "Sad", "😢" },
            { "Cranky", "😡" },
            { "Anxious", "😰" }
        };
        
        public MoodTrackerService()
        {
            // Set up data file path in LocalApplicationData
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BabyEngine" // App-specific folder
            );
            
            Directory.CreateDirectory(appDataPath); // Ensure the directory exists
            _dataFilePath = Path.Combine(appDataPath, "mood_entries.json");
            
            LoadEntries();

             // Set initial mood based on loaded data or default
             if (_entries.Any()) {
                 CurrentMood = _entries.OrderByDescending(e => e.Timestamp).First().Mood;
             } else {
                  CurrentMood = MoodOptions.First().Value; // Default to first mood if none loaded
             }
        }
        
        public void AddMoodEntry(string mood, string note = "") // Note is optional
        {
            // Validate mood against options
            if (!MoodOptions.ContainsValue(mood)) {
                 // Handle invalid mood input if necessary, maybe log or ignore
                 Console.WriteLine($"Warning: Attempted to add invalid mood: {mood}");
                 return; 
            }

            var entry = new MoodEntry
            {
                Mood = mood,
                Note = note,
                Timestamp = DateTime.Now
            };
            
            _entries.Add(entry);
            CurrentMood = mood; // Update current mood immediately
            
            SaveEntries();
            OnPropertyChanged(nameof(Entries)); // Notify if UI binds to the full list
        }
        
        public List<MoodEntry> GetRecentEntries(int count = 5)
        {
            // Return the most recent entries
            return _entries.OrderByDescending(e => e.Timestamp).Take(count).ToList();
        }
        
        private void LoadEntries()
        {
            try
            {
                if (File.Exists(_dataFilePath))
                {
                    string json = File.ReadAllText(_dataFilePath);
                    var loadedEntries = JsonSerializer.Deserialize<List<MoodEntry>>(json);
                    
                    if (loadedEntries != null)
                    {
                        _entries = loadedEntries;
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error loading mood entries (invalid JSON): {ex.Message}");
                // Optionally: Backup corrupt file and start fresh
            }
            catch (IOException ex)
            {
                 Console.WriteLine($"Error reading mood entries file: {ex.Message}");
            }
            catch (Exception ex) // Catch other potential errors
            {
                Console.WriteLine($"Unexpected error loading mood entries: {ex.Message}");
            }
        }
        
        private void SaveEntries()
        {
            try
            {
                string json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions
                {
                    WriteIndented = true // Make the JSON file readable
                });
                
                File.WriteAllText(_dataFilePath, json);
            }
             catch (JsonException ex)
            {
                 Console.WriteLine($"Error serializing mood entries: {ex.Message}");
            }
            catch (IOException ex)
            {
                 Console.WriteLine($"Error writing mood entries file: {ex.Message}");
            }
            catch (Exception ex) // Catch other potential errors
            {
                Console.WriteLine($"Unexpected error saving mood entries: {ex.Message}");
            }
        }
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 