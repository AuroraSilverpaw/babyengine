using System;
using System.Globalization;

namespace LicenseGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8; // Ensure emojis display correctly

            Console.WriteLine("========================================");
            Console.WriteLine("   🧙 Offline License Generator Wizard 🧙 ");
            Console.WriteLine("========================================");
            Console.WriteLine("Welcome! Let's generate a license key.\n");

            // 1. Get API Key
            string apiKey = string.Empty;
            while (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.Write("🔑 Please enter the API Key (e.g., sk-...): ");
                apiKey = Console.ReadLine()?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(apiKey) || !apiKey.StartsWith("sk-"))
                {
                    Console.WriteLine("   ⚠️ Invalid API Key format. It should start with 'sk-'. Please try again.");
                    apiKey = string.Empty; // Reset to loop
                }
            }

            // 2. Get Start Date (Default to today)
            DateTime startDate = DateTime.Today;
            Console.Write($"📅 Enter the license start date (YYYY-MM-DD) [Default: {startDate:yyyy-MM-dd}]: ");
            string startDateInput = Console.ReadLine()?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(startDateInput))
            {
                if (!DateTime.TryParseExact(startDateInput, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate))
                {
                    Console.WriteLine($"   ⚠️ Invalid date format. Using default start date: {DateTime.Today:yyyy-MM-dd}");
                    startDate = DateTime.Today;
                }
            }

            // 3. Get Duration
            int durationDays = 0;
            while (durationDays <= 0)
            {
                Console.Write("⏳ Enter the license duration (in days, e.g., 30): ");
                string durationInput = Console.ReadLine()?.Trim() ?? string.Empty;
                if (!int.TryParse(durationInput, out durationDays) || durationDays <= 0)
                {
                    Console.WriteLine("   ⚠️ Please enter a positive whole number for the duration.");
                    durationDays = 0; // Reset to loop
                }
            }

            // --- Generation ---
            try
            {
                string licenseKey = LicenseLogic.GenerateLicenseKey(apiKey, startDate, durationDays);
                DateTime expiryDate = startDate.AddDays(durationDays);

                Console.WriteLine("\n----------------------------------------");
                Console.WriteLine("   Generating with the following info:    ");
                Console.WriteLine("----------------------------------------");
                Console.WriteLine($" API Key:      {apiKey.Substring(0,5)}...{apiKey.Substring(apiKey.Length-4)}"); // Show partial key
                Console.WriteLine($" Start Date:   {startDate:yyyy-MM-dd}");
                Console.WriteLine($" Duration:     {durationDays} days");
                Console.WriteLine($" Expiry Date:  {expiryDate:yyyy-MM-dd}");
                Console.WriteLine("----------------------------------------\n");

                Console.WriteLine("✨ License Key Generated Successfully! ✨");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($" >> {licenseKey} <<");
                Console.ResetColor();
                Console.WriteLine("========================================\n");
                Console.WriteLine("Copy the license key above and provide it to the user.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n❌ Error generating license key:");
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
} 