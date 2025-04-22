using System;
using System.Timers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows; // Required for Dispatcher

namespace BabyEngine.Models
{
    public class SystemTimeService : INotifyPropertyChanged, IDisposable
    {
        private System.Timers.Timer? _timer;
        private DateTime _currentTime;
        private bool _disposed = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public DateTime CurrentTime
        {
            get => _currentTime;
            private set
            {
                if (_currentTime != value)
                {
                    _currentTime = value;
                    OnPropertyChanged(); // Notify that CurrentTime changed
                    OnPropertyChanged(nameof(TimeString)); // Notify that the formatted string changed
                    OnPropertyChanged(nameof(DateString)); // Notify that the formatted string changed
                }
            }
        }

        // Formatted time string for UI binding
        public string TimeString => CurrentTime.ToString("HH:mm");

        // Formatted date string for UI binding
        public string DateString => CurrentTime.ToString("ddd, MMM d");

        public SystemTimeService()
        {
            // Initialize with current time
            _currentTime = DateTime.Now; // Set initial value before raising PropertyChanged
            
            // Set up timer to update time every second
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += Timer_Elapsed;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
             UpdateTime();
        }

        private void UpdateTime()
        {
             // Ensure updates happen on the UI thread
            if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => CurrentTime = DateTime.Now);
            }
            else
            {
                 // If already on UI thread or Dispatcher not available (e.g., testing), update directly
                 CurrentTime = DateTime.Now;
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Implement IDisposable to clean up the timer
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
                    // Dispose managed state (managed objects)
                    if (_timer != null)
                    {
                        _timer.Elapsed -= Timer_Elapsed;
                        _timer.Stop();
                        _timer.Dispose();
                        _timer = null;
                    }
                }
                // Free unmanaged resources (unmanaged objects) and override finalizer
                // Set large fields to null
                _disposed = true;
            }
        }

        // Finalizer (just in case Dispose is not called)
         ~SystemTimeService() => Dispose(false);
    }
} 