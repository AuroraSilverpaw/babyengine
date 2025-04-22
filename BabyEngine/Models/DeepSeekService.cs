using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using OpenAI.GPT3;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels.ResponseModels;

namespace BabyEngine.Models
{
    public class DeepSeekService
    {
        private readonly OpenAIService _openAIService;
        private readonly Random _random = new Random();
        private double _blushyMessageFrequency = 0.3; // Default value
        
        public DeepSeekService(string apiKey)
        {
            _openAIService = new OpenAIService(new OpenAiOptions
            {
                ApiKey = apiKey,
                BaseDomain = "https://api.deepseek.com"
            });
        }
        
        public void SetBlushyMessageFrequency(double frequency)
        {
            _blushyMessageFrequency = Math.Clamp(frequency, 0.0, 1.0);
        }
        
        public async Task<string> GetResponseAsync(string userMessage, List<ChatMessage> messageHistory)
        {
            try
            {
                // Convert our app's messages to the format expected by the API
                var chatMessages = new List<OpenAI.GPT3.ObjectModels.RequestModels.ChatMessage>();
                
                // System message to set the persona
                string systemPrompt = "You are a loving, nurturing mommy figure. Your responses should be warm, caring, and maternal. Address the user as 'little one', 'sweetie', or similar endearing terms. Sign your messages with '❤️ Mommy'";
                
                // Add blushy behavior instruction based on frequency setting
                if (_random.NextDouble() < _blushyMessageFrequency)
                {
                    systemPrompt += " For this specific response, act a bit flustered, blushy, and show a slightly embarrassed but affectionate demeanor. Use phrases like '*blushes*', '*fidgets nervously*', or show a bit of shyness in your response.";
                }
                
                chatMessages.Add(OpenAI.GPT3.ObjectModels.RequestModels.ChatMessage.FromSystem(systemPrompt));
                
                // Add message history (limited to last 10 messages for example)
                foreach (var message in messageHistory.Count > 10 
                    ? messageHistory.GetRange(messageHistory.Count - 10, 10) 
                    : messageHistory)
                {
                    if (message.IsFromMommy)
                    {
                        chatMessages.Add(OpenAI.GPT3.ObjectModels.RequestModels.ChatMessage.FromAssistant(message.Content));
                    }
                    else
                    {
                        chatMessages.Add(OpenAI.GPT3.ObjectModels.RequestModels.ChatMessage.FromUser(message.Content));
                    }
                }
                
                // Add the new user message
                chatMessages.Add(OpenAI.GPT3.ObjectModels.RequestModels.ChatMessage.FromUser(userMessage));
                
                // Create the request
                var completionResult = await _openAIService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                {
                    Messages = chatMessages,
                    Model = "deepseek-chat",
                    Temperature = 0.7f,
                    MaxTokens = 1000
                });
                
                if (completionResult.Successful)
                {
                    return completionResult.Choices[0].Message.Content;
                }
                else
                {
                    if (completionResult.Error == null)
                    {
                        throw new Exception("Unknown error");
                    }
                    
                    return $"Error: {completionResult.Error.Message}";
                }
            }
            catch (Exception ex)
            {
                return $"Sorry, I couldn't process that right now. {ex.Message} ❤️ Mommy";
            }
        }
    }
} 