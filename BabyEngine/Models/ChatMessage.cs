using System;

namespace BabyEngine.Models
{
    public class ChatMessage
    {
        public string Content { get; set; }
        public bool IsFromMommy { get; set; }
        public DateTime Timestamp { get; set; }

        public ChatMessage(string content, bool isFromMommy)
        {
            Content = content;
            IsFromMommy = isFromMommy;
            Timestamp = DateTime.Now;
        }
    }
} 