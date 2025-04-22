using System;
using System.IO;
using System.Text.Json;

namespace BabyEngine.Models
{
    public class ConfigService
    {
        private const string ConfigFileName = "config.json";
        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BabyEngine",
            ConfigFileName);
            
        public class AppConfig
        {
            // public string ApiKey { get; set; } = string.Empty; // Removed
            public string LicenseKey { get; set; } = string.Empty; // Added
            public double BlushyMessageFrequency { get; set; } = 0.3; // Default frequency is 30%
        }
        
        private AppConfig _config;
        
        public ConfigService()
        {
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
             SaveConfig();
        }
        
        public double GetBlushyMessageFrequency()
        {
            return _config.BlushyMessageFrequency;
        }
        
        public void SetBlushyMessageFrequency(double frequency)
        {
            _config.BlushyMessageFrequency = Math.Clamp(frequency, 0.0, 1.0);
            SaveConfig();
        }
        
        private AppConfig LoadConfig()
        {
            try
            {
                // Ensure directory exists
                string? dirPath = Path.GetDirectoryName(ConfigFilePath);
                if (!string.IsNullOrEmpty(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }
                
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config: {ex.Message}");
                // Consider logging this error properly instead of just Console.WriteLine
            }
            
            return new AppConfig(); // Return default config if file doesn't exist or error occurs
        }
        
        private void SaveConfig()
        {
            try
            {
                // Ensure directory exists
                string? dirPath = Path.GetDirectoryName(ConfigFilePath);
                if (!string.IsNullOrEmpty(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }
                
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