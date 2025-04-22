using System.Collections.Generic;
using BabyEngine.Models; // Assuming ChatMessage is in this namespace

namespace BabyEngine.Models
{
    public class AppState
    {
        public List<ChatMessage> ChatHistory { get; set; } = new List<ChatMessage>();
        public int BlushyMessagesPerHour { get; set; } = 5; // Default value
        public int ContextHistoryLength { get; set; } = 10; // Default value
        // Add other settings here if needed in the future, e.g., CurrentMood
    }
} 