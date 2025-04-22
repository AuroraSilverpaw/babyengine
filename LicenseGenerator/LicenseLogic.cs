using System;
using System.Text;
using System.Security.Cryptography; // Not used for XOR, but good practice for crypto namespace
using System.Globalization;

namespace LicenseGenerator
{
    // Shared logic for both generator and potentially validator (if needed in C#)
    public static class LicenseLogic
    {
        // !!! IMPORTANT: This key MUST match the one used for validation !!!
        private static readonly string XOR_SECRET_KEY = "WigglesSecretCode!123"; // Keep this secret!

        // --- XOR Cipher ---
        private static byte[] XorCipher(byte[] data, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)(data[i] ^ keyBytes[i % keyBytes.Length]);
            }
            return result;
        }

        // --- License Generation ---
        public static string GenerateLicenseKey(string apiKey, DateTime startDate, int durationDays)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("API Key cannot be empty.", nameof(apiKey));
            }
            if (durationDays <= 0)
            {
                 throw new ArgumentException("Duration must be positive.", nameof(durationDays));
            }

            // 1. Calculate expiry timestamp (Unix timestamp)
            DateTime expiryDate = startDate.AddDays(durationDays);
            // Ensure we are dealing with UTC to avoid timezone issues with Unix Timestamps
            long expiryTimestamp = ((DateTimeOffset)expiryDate.ToUniversalTime()).ToUnixTimeSeconds();

            // 2. Create plain data string: API_KEY|EXPIRY_TIMESTAMP
            string plainDataString = $"{apiKey}|{expiryTimestamp}";
            byte[] plainDataBytes = Encoding.UTF8.GetBytes(plainDataString);

            // 3. XOR Cipher the plain data
            byte[] xorData = XorCipher(plainDataBytes, XOR_SECRET_KEY);

            // 4. Base64 Encode the XORed data
            string licenseKey = Convert.ToBase64String(xorData);

            return licenseKey;
        }

        // --- License Validation (also needed in the main app) ---
        public class LicenseInfo
        {
            public string ApiKey { get; set; } = string.Empty;
            public DateTime ExpiryDateUtc { get; set; } = DateTime.MinValue;
            public bool IsValid { get; set; } = false;
            public string ErrorMessage { get; set; } = string.Empty;
        }

        public static LicenseInfo ValidateLicenseKey(string licenseKey)
        {
            var info = new LicenseInfo();
            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                 info.ErrorMessage = "License key cannot be empty.";
                 return info;
            }

            try
            {
                // 1. Base64 Decode
                byte[] xorData = Convert.FromBase64String(licenseKey);

                // 2. XOR Decrypt
                byte[] plainDataBytes = XorCipher(xorData, XOR_SECRET_KEY);
                string plainDataString = Encoding.UTF8.GetString(plainDataBytes);

                // 3. Parse the decrypted string (API_KEY|EXPIRY_TIMESTAMP)
                int separatorIndex = plainDataString.IndexOf('|');
                if (separatorIndex == -1)
                {
                    info.ErrorMessage = "Invalid license key format (missing separator).";
                    return info;
                }

                info.ApiKey = plainDataString.Substring(0, separatorIndex);
                string timestampString = plainDataString.Substring(separatorIndex + 1);

                if (!long.TryParse(timestampString, out long expiryTimestamp))
                {
                     info.ErrorMessage = "Invalid license key format (invalid timestamp).";
                     return info;
                }
                 info.ExpiryDateUtc = DateTimeOffset.FromUnixTimeSeconds(expiryTimestamp).UtcDateTime;


                // 4. Validate expiry time against current time (UTC)
                if (DateTime.UtcNow <= info.ExpiryDateUtc)
                {
                    info.IsValid = true;
                }
                else
                {
                    info.ErrorMessage = $"License expired on {info.ExpiryDateUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}.";
                }

                // Validate API Key basic format (optional, example: starts with sk-)
                 if (string.IsNullOrWhiteSpace(info.ApiKey) || !info.ApiKey.StartsWith("sk-")) {
                      info.IsValid = false; // Mark as invalid even if not expired
                      info.ErrorMessage = "Invalid API Key format within license.";
                 }

            }
            catch (FormatException)
            {
                 info.ErrorMessage = "Invalid Base64 format in license key.";
            }
            catch (Exception ex) // Catch other potential errors
            {
                 info.ErrorMessage = $"An unexpected error occurred during validation: {ex.Message}";
                 // Optionally log the full exception ex here
            }

            return info;
        }
    }
} 