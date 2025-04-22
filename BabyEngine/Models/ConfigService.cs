using System;
using System.IO;
using System.Text.Json;
using BabyEngine.Models; // Required for AppState
using System.Reflection; // Needed for Assembly location

namespace BabyEngine.Models
{
    public class ConfigService
    {
        private const string ConfigFileName = "config.json";
        private const string StateFileName = "app_state.json";
        
        // Get directory of the executable
        private static readonly string ExecutableDirectoryPath = 
            Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location ?? string.Empty) ?? AppContext.BaseDirectory ?? string.Empty;

        private static readonly string ConfigFilePath = Path.Combine(ExecutableDirectoryPath, ConfigFileName);
        private static readonly string StateFilePath = Path.Combine(ExecutableDirectoryPath, StateFileName);
            
        public class AppConfig
        {
            // public string ApiKey { get; set; } = string.Empty; // Removed
            public string LicenseKey { get; set; } = string.Empty; // Added
            // public double BlushyMessageFrequency { get; set; } = 0.3; // Changed
            public int BlushyMessagesPerHour { get; set; } = 5; // Default 5 messages/hour
        }
        
        private AppConfig _config;
        
        public ConfigService()
        {
            // Directory should exist, no need to create
            _config = LoadConfig();
        }
        
        /* // Removed API Key methods
        public string GetApiKey()
        {
            return _config.ApiKey;
        }
        
        public void SetApiKey(string apiKey)
        {
            _config.ApiKey = apiKey;
            SaveConfig();
        }
        */
        
        public string GetLicenseKey()
        {
             return _config.LicenseKey;
        }

        public void SetLicenseKey(string licenseKey)
        {
             _config.LicenseKey = licenseKey;
             SaveConfig(); // Save the config file when license changes
        }
        
        // public double GetBlushyMessageFrequency() // Renamed and changed return type
        public int GetBlushyMessagesPerHour()
        {
            // return _config.BlushyMessageFrequency;
            return _config.BlushyMessagesPerHour;
        }
        
        // public void SetBlushyMessageFrequency(double frequency) // Renamed and changed parameter type
        public void SetBlushyMessagesPerHour(int messagesPerHour)
        {
            // _config.BlushyMessageFrequency = Math.Clamp(frequency, 0.0, 1.0);
            // Clamp between 1 and 30 as defined in ViewModel
            _config.BlushyMessagesPerHour = Math.Max(1, Math.Min(30, messagesPerHour));
            SaveConfig(); // Save the config file when blushy freq changes
        }
        
        // --- App State Persistence Methods ---

        public AppState LoadAppState()
        {
            try
            {
                if (File.Exists(StateFilePath))
                {
                    string json = File.ReadAllText(StateFilePath);
                    // Use JsonSerializerDefaults.Web for potentially more resilient deserialization
                    var loadedState = JsonSerializer.Deserialize<AppState>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return loadedState ?? new AppState(); // Return default if null
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading app state: {ex.Message}");
                // Consider logging this error properly
            }

            return new AppState(); // Return default state if file doesn't exist or error occurs
        }

        public void SaveAppState(AppState state)
        {
            Console.WriteLine($"[DEBUG] Attempting to save app state to: {StateFilePath}"); // Log path
            try
            {
                string json = JsonSerializer.Serialize(state, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(StateFilePath, json);
                Console.WriteLine("[DEBUG] Successfully wrote app state file."); // Log success
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Error saving app state: {ex.Message}"); // Log error
                // Consider logging this error properly
            }
        }

        // --- Private Config Load/Save ---
        // (Keep existing LoadConfig and SaveConfig methods for the config.json file)
        private AppConfig LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config: {ex.Message}");
            }
            return new AppConfig();
        }
        
        private void SaveConfig()
        {
            try
            {
                string json = JsonSerializer.Serialize(_config, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving config: {ex.Message}");
                // Consider logging this error properly
            }
        }
    }
} 