using System;
using System.Text;
using System.Globalization;

namespace BabyEngine.Models
{
    // Service responsible for validating license keys within the BabyEngine application.
    public static class LicenseService
    {
        // !!! IMPORTANT: This key MUST match the one used in the generator !!!
        private static readonly string XOR_SECRET_KEY = "WigglesSecretCode!123";

        // Information extracted from a validated license key.
        public class LicenseInfo
        {
            public string ApiKey { get; internal set; } = string.Empty;
            public DateTime ExpiryDateUtc { get; internal set; } = DateTime.MinValue;
            public bool IsValid { get; internal set; } = false;
            public string ErrorMessage { get; internal set; } = string.Empty;

            // Helper property for display
            public string StatusMessage => IsValid 
                ? $"License valid until {ExpiryDateUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}" 
                : (string.IsNullOrEmpty(ErrorMessage) ? "Invalid license." : ErrorMessage);
        }

        // --- XOR Cipher (Identical to generator) ---
        private static byte[] XorCipher(byte[] data, string key)
        { 
            if (data == null || data.Length == 0 || string.IsNullOrEmpty(key)) return data ?? Array.Empty<byte>();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            if (keyBytes.Length == 0) return data; // Avoid division by zero
            byte[] result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)(data[i] ^ keyBytes[i % keyBytes.Length]);
            }
            return result;
        }

        // Validates the provided license key string.
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
                // Use padding-tolerant decoding if needed, though standard should work if generated correctly
                byte[] xorData = Convert.FromBase64String(licenseKey);

                // 2. XOR Decrypt
                byte[] plainDataBytes = XorCipher(xorData, XOR_SECRET_KEY);
                 if (plainDataBytes == null || plainDataBytes.Length == 0) {
                      info.ErrorMessage = "Decryption resulted in empty data.";
                      return info;
                 }
                 string plainDataString = Encoding.UTF8.GetString(plainDataBytes);

                // 3. Parse the decrypted string (API_KEY|EXPIRY_TIMESTAMP)
                int separatorIndex = plainDataString.IndexOf('|');
                if (separatorIndex == -1)
                {
                    info.ErrorMessage = "Invalid license format (missing separator).";
                    return info;
                }

                string apiKeyCandidate = plainDataString.Substring(0, separatorIndex);
                string timestampString = plainDataString.Substring(separatorIndex + 1);

                 // Basic API key format check
                 if (string.IsNullOrWhiteSpace(apiKeyCandidate) || !apiKeyCandidate.StartsWith("sk-")) {
                      info.ErrorMessage = "Invalid API Key format in license.";
                      return info;
                 }
                 info.ApiKey = apiKeyCandidate;

                 // Timestamp parsing
                if (!long.TryParse(timestampString, out long expiryTimestamp))
                {
                     info.ErrorMessage = "Invalid license format (invalid timestamp).";
                     return info;
                }
                try {
                     info.ExpiryDateUtc = DateTimeOffset.FromUnixTimeSeconds(expiryTimestamp).UtcDateTime;
                } catch (ArgumentOutOfRangeException) {
                    info.ErrorMessage = "Invalid timestamp value in license.";
                    return info;
                }

                // 4. Validate expiry time against current time (UTC)
                if (DateTime.UtcNow <= info.ExpiryDateUtc)
                {
                    info.IsValid = true;
                    info.ErrorMessage = string.Empty; // Clear any previous errors if now valid
                }
                else
                {
                    info.IsValid = false; // Ensure IsValid is false if expired
                    info.ErrorMessage = $"License expired on {info.ExpiryDateUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}.";
                }
            }
            catch (FormatException)
            {
                 info.ErrorMessage = "Invalid Base64 format in license key.";
            }
            catch (Exception ex) // Catch other potential errors during parsing/decryption
            {
                 info.ErrorMessage = $"Error during license validation: {ex.Message}";
                 // Consider logging the full exception `ex` for debugging
            }

            return info;
        }
    }
} 