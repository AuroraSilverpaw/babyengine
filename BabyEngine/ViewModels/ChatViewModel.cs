using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BabyEngine.Models;
using System.Threading.Tasks;
using System.Windows;
using System.Globalization;
using System.Linq;
using System.Windows.Threading;

namespace BabyEngine.ViewModels
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        private string _inputMessage = string.Empty;
        private bool _isPaused;
        private readonly Random _random = new Random();
        private readonly DeepSeekService _deepSeekService;
        private readonly ConfigService _configService;
        private readonly SystemTimeService _systemTimeService;
        private readonly BatteryService _batteryService;
        private readonly MoodTrackerService _moodTrackerService;
        private readonly ReminderService _reminderService;
        private readonly AchievementService _achievementService;
        private bool _isProcessing;
        private int _blushyMessagesPerHour;
        private int _contextHistoryLength;
        private readonly LicenseService.LicenseInfo _licenseInfo;
        private DispatcherTimer? _blushyTimer;
        
        // Messages collection
        public ObservableCollection<ChatMessage> Messages { get; } = new ObservableCollection<ChatMessage>();
        
        // Built-in blushy messages list
        private readonly List<string> _blushyMessages = new List<string>
        {
            "*blushes softly* H-hi there... >.<",
            "Mommy thinks you're really cute... *looks away shyly*",
            "Eep! D-did Mommy say that out loud? *covers face*",
            "*wiggles happily* Just thinking about my sweet little one...",
            "You always make Mommy feel so warm and fuzzy inside~ UwU",
            "*nuzzles* You're Mommy's favorite little fluffball.",
            "Is... is it warm in here or is it just you? *fans self*",
            "*purrs softly* Mommy likes spending time with you..."
        };
        
        // Commands
        public ICommand SendMessageCommand { get; }
        public ICommand TogglePauseCommand { get; }
        public ICommand AddReminderCommand { get; }
        public ICommand SetMoodCommand { get; }
        
        // Properties from the system time service
        public string CurrentTime => _systemTimeService.TimeString;
        public string CurrentDate => _systemTimeService.DateString;
        
        // Properties from the battery service
        public string BatteryIcon => _batteryService.BatteryIcon;
        public string BatteryStatus => _batteryService.BatteryStatus;
        
        // Property for current mood
        public string CurrentMood => _moodTrackerService.CurrentMood;
        
        // Property for reminders
        public List<Reminder> ActiveReminders => _reminderService.ActiveReminders;
        
        // Expose license status for potential display in UI
        public string LicenseStatus => _licenseInfo?.StatusMessage ?? "License status unknown";
        
        // Method to allow UI to potentially refresh the displayed status
        public void RefreshLicenseStatus()
        {
            OnPropertyChanged(nameof(LicenseStatus));
        }
        
        public int BlushyMessagesPerHour
        {
            get => _blushyMessagesPerHour;
            set
            {
                int clampedValue = Math.Max(1, Math.Min(30, value));
                if (_blushyMessagesPerHour != clampedValue)
                {
                    _blushyMessagesPerHour = clampedValue;
                    _configService.SetBlushyMessagesPerHour(clampedValue);
                    SetupBlushyTimer();
                    OnPropertyChanged();
                }
            }
        }
        
        public int ContextHistoryLength
        {
            get => _contextHistoryLength;
            set
            {
                int clampedValue = Math.Max(1, Math.Min(50, value));
                if (_contextHistoryLength != clampedValue)
                {
                    _contextHistoryLength = clampedValue;
                    OnPropertyChanged();
                }
            }
        }
        
        public string InputMessage
        {
            get => _inputMessage;
            set
            {
                _inputMessage = value;
                OnPropertyChanged();
            }
        }
        
        public bool IsPaused
        {
            get => _isPaused;
            set
            {
                _isPaused = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PauseButtonText));
            }
        }
        
        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                _isProcessing = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }
        
        public string PauseButtonText => IsPaused ? "Resume" : "Pause";
        
        // Example messages as fallback
        private readonly string[] _mommyMessages = new[]
        {
            "Hello there, little one! Mommy is here now. What would you like to talk about today? ❤️ Mommy",
            "Did you remember to drink your water today? Mommy wants you to stay hydrated!",
            "You're such a good little one. Mommy is so proud of you!",
            "Do you need Mommy to take care of anything for you, sweetie?",
            "Mommy thinks you deserve extra cuddles today. You've been so well-behaved!",
            "Is my little one feeling shy today? That's okay, Mommy understands.",
            "Remember that Mommy loves you very much, no matter what!"
        };
        
        public ChatViewModel(LicenseService.LicenseInfo licenseInfo, AppState loadedState)
        {
            _licenseInfo = licenseInfo ?? throw new ArgumentNullException(nameof(licenseInfo));
            if (!_licenseInfo.IsValid || string.IsNullOrEmpty(_licenseInfo.ApiKey)) {
                 throw new InvalidOperationException("ChatViewModel cannot be created without a valid license and API key.");
            }

            // Initialize services
            _configService = new ConfigService();
            _deepSeekService = new DeepSeekService(_licenseInfo.ApiKey);
            _systemTimeService = new SystemTimeService();
            _batteryService = new BatteryService();
            _moodTrackerService = new MoodTrackerService();
            _reminderService = new ReminderService();
            _achievementService = new AchievementService();
            
            // Initialize from loaded state
            Messages = new ObservableCollection<ChatMessage>(loadedState.ChatHistory);
            _blushyMessagesPerHour = loadedState.BlushyMessagesPerHour;
            _contextHistoryLength = loadedState.ContextHistoryLength;
            SetupBlushyTimer();
            
            // Subscribe to property changed events
            _systemTimeService.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_systemTimeService.TimeString))
                {
                    OnPropertyChanged(nameof(CurrentTime));
                }
                if (e.PropertyName == nameof(_systemTimeService.DateString))
                {
                    OnPropertyChanged(nameof(CurrentDate));
                }
            };
            
            _batteryService.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_batteryService.BatteryIcon))
                {
                    OnPropertyChanged(nameof(BatteryIcon));
                }
                if (e.PropertyName == nameof(_batteryService.BatteryStatus))
                {
                    OnPropertyChanged(nameof(BatteryStatus));
                }
            };
            
            _moodTrackerService.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_moodTrackerService.CurrentMood))
                {
                    OnPropertyChanged(nameof(CurrentMood));
                }
            };
            
            _reminderService.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_reminderService.ActiveReminders))
                {
                    OnPropertyChanged(nameof(ActiveReminders));
                }
            };
            
            // Subscribe to reminder due events
            _reminderService.ReminderDue += OnReminderDue;
            
            // Subscribe to achievement unlocked events
            _achievementService.AchievementUnlocked += OnAchievementUnlocked;
            
            // Only add welcome message if history is empty
            if (!Messages.Any())
            {
                Messages.Add(new ChatMessage(_mommyMessages[_random.Next(_mommyMessages.Length)], true));
                // Check for first chat achievement only if starting fresh
                Task.Delay(2000).ContinueWith(_ =>
                {
                     Application.Current?.Dispatcher.Invoke(() => {
                          var firstChatAchievement = _achievementService.Achievements.FirstOrDefault(a => a.Title == "First Chat");
                          if (firstChatAchievement != null && !firstChatAchievement.IsUnlocked)
                          {
                               _achievementService.UnlockAchievement(firstChatAchievement.Id.ToString());
                          }
                     });
                }, TaskScheduler.Default);
            }
            
            // Initialize commands
            SendMessageCommand = new RelayCommand(async () => await SendMessageAsync(), CanSendMessage);
            TogglePauseCommand = new RelayCommand(TogglePause);
            AddReminderCommand = new RelayCommand<string>(AddReminder);
            SetMoodCommand = new RelayCommand<string>(SetMood);
        }
        
        private void SetupBlushyTimer()
        {
            if (_blushyTimer != null)
            {
                _blushyTimer.Stop();
                _blushyTimer.Tick -= BlushyTimer_Tick;
            }

            if (_blushyMessagesPerHour > 0 && _blushyMessages.Any())
            {
                _blushyTimer = new DispatcherTimer();
                _blushyTimer.Interval = TimeSpan.FromHours(1.0 / _blushyMessagesPerHour);
                _blushyTimer.Tick += BlushyTimer_Tick;
                _blushyTimer.Start();
            }
        }

        private void BlushyTimer_Tick(object? sender, EventArgs e)
        {
            if (Messages.Count > 0 && _blushyMessages.Any())
            {
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    string randomBlushyMessage = _blushyMessages[_random.Next(_blushyMessages.Count)];
                    Messages.Add(new ChatMessage(randomBlushyMessage, true));
                });
            }
        }
        
        private async Task SendMessageAsync()
        {
            if (!CanSendMessage()) return;
                
            string userMessage = InputMessage;
            InputMessage = string.Empty;
            
            Messages.Add(new ChatMessage(userMessage, false));
            
            CheckForAchievements(userMessage);
            
            if (!IsPaused)
            {
                IsProcessing = true;
                CommandManager.InvalidateRequerySuggested();
                
                try
                {
                    // Prepare message history based on ContextHistoryLength
                    List<ChatMessage> messageHistory = Messages.Skip(Math.Max(0, Messages.Count - ContextHistoryLength)).ToList();
                    
                    string aiResponse = await _deepSeekService.GetResponseAsync(userMessage, messageHistory);
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Messages.Add(new ChatMessage(aiResponse, true));
                    });
                }
                catch (Exception ex)
                {
                     // Log the exception ex details for debugging
                     Console.WriteLine($"Error getting AI response: {ex}"); 
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        string fallbackMessage = $"Sorry, little one. Mommy had a little oopsie connecting... ({ex.Message}) ❤️ Mommy";
                        Messages.Add(new ChatMessage(fallbackMessage, true));
                    });
                }
                finally
                {
                    IsProcessing = false;
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private void CheckForAchievements(string userMessage)
        {
             // Check for bedtime story achievement
            if (userMessage.ToLower().Contains("bedtime story") || 
                (userMessage.ToLower().Contains("story") && userMessage.ToLower().Contains("bed")))
            {
                var bedtimeStoryAchievement = _achievementService.Achievements.FirstOrDefault(a => a.Title == "Bedtime Story");
                if (bedtimeStoryAchievement != null && !bedtimeStoryAchievement.IsUnlocked)
                {
                    _achievementService.UnlockAchievement(bedtimeStoryAchievement.Id.ToString());
                }
            }
            
            // Check for deep conversation achievement (if chat has more than 10 messages)
            // Note: This might trigger frequently after 10 messages. Consider a flag or different trigger.
            if (Messages.Count > 10)
            {
                var deepConversationAchievement = _achievementService.Achievements.FirstOrDefault(a => a.Title == "Deep Conversation");
                if (deepConversationAchievement != null && !deepConversationAchievement.IsUnlocked)
                {
                    _achievementService.UnlockAchievement(deepConversationAchievement.Id.ToString());
                }
            }
        }
        
        private bool CanSendMessage()
        {
            return !IsProcessing && !string.IsNullOrWhiteSpace(InputMessage);
        }
        
        private void OnReminderDue(object? sender, Reminder reminder)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                string reminderMessage = $"Reminder, little one: {reminder.Title} - {reminder.Message} ❤️ Mommy";
                Messages.Add(new ChatMessage(reminderMessage, true));
            });
        }
        
        private void OnAchievementUnlocked(object? sender, Achievement achievement)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                string achievementMessage = $"Congratulations, sweetie! You've earned the '{achievement.Title}' achievement! {achievement.Icon} ❤️ Mommy";
                Messages.Add(new ChatMessage(achievementMessage, true));
            });
        }
        
        private void AddReminder(string? parameters)
        {
            if (string.IsNullOrEmpty(parameters)) return;
            
            string[] parts = parameters.Split('|');
            if (parts.Length < 3) return;
            
            string title = parts[0];
            string message = parts[1];
            if (DateTime.TryParse(parts[2], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime dueTime))
            {
                bool isRecurring = parts.Length > 3 && bool.TryParse(parts[3], out bool recurring) && recurring;
                string recurrencePattern = parts.Length > 4 ? parts[4] : "";
                
                _reminderService.AddReminder(title, message, dueTime, isRecurring, recurrencePattern);
                
                string confirmationMessage = $"I've set a reminder for {dueTime.ToLocalTime():g} about '{title}', sweetie! ❤️ Mommy";
                Messages.Add(new ChatMessage(confirmationMessage, true));
            }
        }
        
        private void SetMood(string? mood)
        {
            if (string.IsNullOrEmpty(mood)) return;
            
            _moodTrackerService.AddMoodEntry(mood, "");
            
            string moodDisplayName = MoodTrackerService.MoodOptions.FirstOrDefault(kvp => kvp.Value == mood).Key ?? mood;

            string moodMessage = $"I see you're feeling {mood} today, little one ({moodDisplayName}). Thank you for sharing! ❤️ Mommy";
            Messages.Add(new ChatMessage(moodMessage, true));
        }
        
        private void TogglePause()
        {
            IsPaused = !IsPaused;
             string pauseMsg = IsPaused ? "Mommy will pause chatting for a bit, okay sweetie? Let me know when you want to resume. ❤️ Mommy" 
                                        : "Okay, Mommy is back and listening! ❤️ Mommy";
             Messages.Add(new ChatMessage(pauseMsg, true));
        }
        
        // Method to get current state for saving
        public AppState GetCurrentState()
        {
            return new AppState
            {
                ChatHistory = new List<ChatMessage>(this.Messages), // Create a new list from the observable collection
                BlushyMessagesPerHour = this.BlushyMessagesPerHour,
                ContextHistoryLength = this.ContextHistoryLength
                // Add other properties to save if needed
            };
        }
        
        #region INotifyPropertyChanged
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        #endregion
    }
    
    // Simple relay command implementation
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;
        
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        
        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
        
        public void Execute(object? parameter) => _execute();
        
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
    
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Predicate<T?>? _canExecute;
        
        public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        
        public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
        
        public void Execute(object? parameter) => _execute((T?)parameter);
        
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
} 