using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Timers;
using System.Windows; // For Dispatcher

namespace BabyEngine.Models
{
    public class Reminder : INotifyPropertyChanged
    {
        private string _title = "";
        private string _message = "";
        private DateTime _dueTime;
        private bool _isCompleted;
        private bool _isRecurring;
        private string _recurrencePattern = ""; // e.g., "Daily", "Weekly", etc. Can be expanded.
        private Guid _id = Guid.NewGuid();

        public string Title { get => _title; set => SetProperty(ref _title, value); }
        public string Message { get => _message; set => SetProperty(ref _message, value); }
        public DateTime DueTime { get => _dueTime; set => SetProperty(ref _dueTime, value); }
        public bool IsCompleted { get => _isCompleted; set => SetProperty(ref _isCompleted, value); }
        public bool IsRecurring { get => _isRecurring; set => SetProperty(ref _isRecurring, value); }
        public string RecurrencePattern { get => _recurrencePattern; set => SetProperty(ref _recurrencePattern, value); }
        public Guid Id { get => _id; set => SetProperty(ref _id, value); } // Should generally not change ID

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    public class ReminderService : INotifyPropertyChanged, IDisposable
    {
        private readonly string _dataFilePath;
        private List<Reminder> _reminders = new List<Reminder>();
        private System.Timers.Timer? _checkTimer;
        private bool _disposed = false;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<Reminder>? ReminderDue; // Event raised when a reminder is due
        
        // Provides a copy of the reminders list
        public List<Reminder> Reminders => new List<Reminder>(_reminders);
        
        // Provides currently active (not completed) reminders, ordered by due time
        public List<Reminder> ActiveReminders => _reminders
            .Where(r => !r.IsCompleted)
            .OrderBy(r => r.DueTime)
            .ToList();
        
        public ReminderService()
        {
            // Set up data file path
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BabyEngine"
            );
            
            Directory.CreateDirectory(appDataPath);
            _dataFilePath = Path.Combine(appDataPath, "reminders.json");
            
            LoadReminders();
            
            // Set up timer to check for due reminders (e.g., every 30 seconds)
            _checkTimer = new System.Timers.Timer(30000); // Check every 30 seconds
            _checkTimer.Elapsed += CheckTimer_Elapsed;
            _checkTimer.AutoReset = true;
            _checkTimer.Start();
        }

        private void CheckTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            CheckDueReminders();
        }
        
        public void AddReminder(string title, string message, DateTime dueTime, bool isRecurring = false, string recurrencePattern = "")
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title cannot be empty", nameof(title));

            var reminder = new Reminder
            {
                Title = title,
                Message = message,
                DueTime = dueTime,
                IsRecurring = isRecurring,
                RecurrencePattern = recurrencePattern,
                IsCompleted = false // New reminders are not completed
            };
            
            _reminders.Add(reminder);
            SaveReminders();
            OnPropertyChanged(nameof(ActiveReminders)); // Notify UI that the active list changed
        }

         public void CompleteReminder(Guid reminderId)
         {
              var reminder = _reminders.FirstOrDefault(r => r.Id == reminderId);
              if (reminder != null && !reminder.IsCompleted)
              {
                   if (!reminder.IsRecurring)
                   {
                        reminder.IsCompleted = true;
                   }
                   else
                   {
                        // Reschedule recurring reminder (simple example: add interval based on pattern)
                         // This needs more robust logic based on RecurrencePattern
                         if (reminder.RecurrencePattern.Equals("Daily", StringComparison.OrdinalIgnoreCase)) {
                              reminder.DueTime = reminder.DueTime.AddDays(1);
                         } else if (reminder.RecurrencePattern.Equals("Weekly", StringComparison.OrdinalIgnoreCase)) {
                              reminder.DueTime = reminder.DueTime.AddDays(7);
                         } else {
                              // Default or unknown pattern: Mark complete for now
                              reminder.IsCompleted = true; 
                         }
                   }
                   SaveReminders();
                   OnPropertyChanged(nameof(ActiveReminders));
              }
         }

          public void DeleteReminder(Guid reminderId)
         {
              int removedCount = _reminders.RemoveAll(r => r.Id == reminderId);
              if (removedCount > 0) {
                   SaveReminders();
                   OnPropertyChanged(nameof(ActiveReminders));
              }
         }

        private void CheckDueReminders()
        {
            DateTime now = DateTime.Now;
            // Important: Iterate over a copy or use indices if modifying the list during iteration
            var dueReminders = _reminders.Where(r => !r.IsCompleted && r.DueTime <= now).ToList();

            if (dueReminders.Any()) {
                // Use Dispatcher to raise events on the UI thread
                Application.Current?.Dispatcher.Invoke(() =>
                {
                     foreach (var reminder in dueReminders)
                     {
                          ReminderDue?.Invoke(this, reminder); // Raise the event
                          // Mark as complete or reschedule based on recurrence
                           CompleteReminder(reminder.Id); 
                     }
                });
            }
        }
        
        private void LoadReminders()
        {
            try
            {
                if (File.Exists(_dataFilePath))
                {
                    string json = File.ReadAllText(_dataFilePath);
                    var loadedReminders = JsonSerializer.Deserialize<List<Reminder>>(json);
                    
                    if (loadedReminders != null)
                    {
                        _reminders = loadedReminders;
                        // Optionally remove completed non-recurring reminders from the past on load
                        _reminders.RemoveAll(r => r.IsCompleted && !r.IsRecurring && r.DueTime < DateTime.Now.AddDays(-7)); // Example: Clean up old completed tasks
                    }
                }
            }
             catch (JsonException ex)
            {
                Console.WriteLine($"Error loading reminders (invalid JSON): {ex.Message}");
            }
            catch (IOException ex)
            {
                 Console.WriteLine($"Error reading reminders file: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error loading reminders: {ex.Message}");
            }
            // Always notify after load attempt, even if empty
             OnPropertyChanged(nameof(ActiveReminders)); 
        }
        
        private void SaveReminders()
        {
            try
            {
                string json = JsonSerializer.Serialize(_reminders, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                File.WriteAllText(_dataFilePath, json);
            }
            catch (JsonException ex)
            {
                 Console.WriteLine($"Error serializing reminders: {ex.Message}");
            }
            catch (IOException ex)
            {
                 Console.WriteLine($"Error writing reminders file: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error saving reminders: {ex.Message}");
            }
        }
        
        // Method to raise PropertyChanged event
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Implement IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                     if (_checkTimer != null)
                    {
                         _checkTimer.Elapsed -= CheckTimer_Elapsed;
                         _checkTimer.Stop();
                         _checkTimer.Dispose();
                         _checkTimer = null;
                    }
                }
                _disposed = true;
            }
        }

         ~ReminderService() => Dispose(false);
    }
} 