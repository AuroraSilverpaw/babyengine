using System;
using System.Runtime.InteropServices; // For P/Invoke if using system calls
using System.Timers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace BabyEngine.Models
{
    // NOTE: Getting accurate battery info reliably across all Windows versions 
    // can be tricky. This uses a basic approach. A more robust solution might
    // involve WMI (Windows Management Instrumentation) or more complex Win32 API calls.

    public class BatteryService : INotifyPropertyChanged, IDisposable
    {
        // Basic structure for battery status from Win32 API (simplified)
        [StructLayout(LayoutKind.Sequential)]
        public struct SystemPowerStatus
        {
            public byte ACLineStatus;             // 0=Offline, 1=Online, 255=Unknown
            public byte BatteryFlag;              // 1=High, 2=Low, 4=Critical, 8=Charging, 128=No system battery, 255=Unknown
            public byte BatteryLifePercent;       // 0-100, 255=Unknown
            public byte Reserved1;                // System Reserved - Must be zero.
            public uint BatteryLifeTime;          // Seconds remaining, 0xFFFFFFFF=Unknown
            public uint BatteryFullLifeTime;      // Seconds when fully charged, 0xFFFFFFFF=Unknown
        }

        // P/Invoke declaration for GetSystemPowerStatus
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemPowerStatus(out SystemPowerStatus lpSystemPowerStatus);

        private System.Timers.Timer? _timer;
        private int _batteryPercentage = 100; // Default to 100% initially
        private bool _isCharging = false;
        private TimeSpan _batteryLifeRemaining = TimeSpan.MaxValue; // Default to unknown/max
        private bool _disposed = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int BatteryPercentage
        {
            get => _batteryPercentage;
            private set
            {
                if (_batteryPercentage != value)
                {
                    _batteryPercentage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BatteryIcon)); // Icon might change
                    OnPropertyChanged(nameof(BatteryStatus)); // Status string changes
                }
            }
        }

        public bool IsCharging
        {
            get => _isCharging;
            private set
            {
                if (_isCharging != value)
                {
                    _isCharging = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BatteryIcon)); // Icon might change
                    OnPropertyChanged(nameof(BatteryStatus)); // Status string changes
                }
            }
        }

        public TimeSpan BatteryLifeRemaining
        {
            get => _batteryLifeRemaining;
            private set
            {
                if (_batteryLifeRemaining != value)
                {
                    _batteryLifeRemaining = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BatteryStatus)); // Status string changes
                }
            }
        }

        // Determines the battery icon based on state
        public string BatteryIcon
        {
            get
            {
                if (IsCharging) return "🔌"; // Charging icon
                if (BatteryPercentage == 255) return "❓"; // Unknown status
                if (BatteryPercentage > 75) return "🔋"; // Full/High
                if (BatteryPercentage > 25) return "🔋"; // Medium (same icon for now)
                if (BatteryPercentage > 10) return "🪫"; // Low
                return "🪫"; // Critical (same icon for now)
            }
        }

        // Provides a formatted status string for the UI
        public string BatteryStatus
        {
            get
            {
                if (BatteryPercentage == 255) return "Unknown"; // Unknown status
                
                string status = $"{BatteryPercentage}%";
                if (IsCharging)
                {
                    status += " Charging";
                }
                else if (BatteryLifeRemaining != TimeSpan.MaxValue && BatteryLifeRemaining.TotalSeconds > 0)
                {
                    int hoursRemaining = (int)BatteryLifeRemaining.TotalHours;
                    int minutesRemaining = BatteryLifeRemaining.Minutes;
                    
                    if (hoursRemaining > 0)
                    {
                        status += $" ({hoursRemaining}h {minutesRemaining}m left)";
                    }
                    else if (minutesRemaining > 0)
                    {
                        status += $" ({minutesRemaining}m left)";
                    }
                    // else: Don't show time if less than a minute or unknown
                }
                 // else: Just show percentage if not charging and time unknown/zero

                return status;
            }
        }

        public BatteryService()
        {
            // Get initial status
            UpdateBatteryInfo();
            
            // Set up timer to update battery info periodically (e.g., every 30 seconds)
            _timer = new System.Timers.Timer(30000); // 30 seconds interval
            _timer.Elapsed += Timer_Elapsed;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

         private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
             // Update on the UI thread as it involves PropertyChanged
             if (Application.Current != null) { // Check if Application exists
                 Application.Current.Dispatcher.Invoke(UpdateBatteryInfo);
             } else {
                 // Fallback if dispatcher not available (e.g. tests, or shutdown)
                 UpdateBatteryInfo(); 
             }
        }

        private void UpdateBatteryInfo()
        {
            try
            {
                if (GetSystemPowerStatus(out SystemPowerStatus status))
                {
                    // Update charging status (ACLineStatus: 1 = online)
                    IsCharging = (status.ACLineStatus == 1);

                    // Update percentage (BatteryLifePercent: 255 = unknown)
                    BatteryPercentage = (status.BatteryLifePercent == 255) ? 255 : status.BatteryLifePercent;

                    // Update remaining time (BatteryLifeTime: 0xFFFFFFFF = unknown)
                    if (status.BatteryLifeTime == 0xFFFFFFFF || IsCharging) // Don't show remaining time if charging or unknown
                    {
                        BatteryLifeRemaining = TimeSpan.MaxValue; // Use MaxValue to indicate unknown/not applicable
                    }
                    else
                    {
                        BatteryLifeRemaining = TimeSpan.FromSeconds(status.BatteryLifeTime);
                    }
                }
                else
                {
                    // Failed to get status - log error or set defaults
                    Console.WriteLine("Failed to get system power status.");
                    BatteryPercentage = 255; // Indicate unknown
                    IsCharging = false;
                    BatteryLifeRemaining = TimeSpan.MaxValue;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating battery info: {ex.Message}");
                 // Set to defaults on error
                 BatteryPercentage = 255; 
                 IsCharging = false;
                 BatteryLifeRemaining = TimeSpan.MaxValue;
            }
        }

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
                    if (_timer != null)
                    {
                        _timer.Elapsed -= Timer_Elapsed;
                        _timer.Stop();
                        _timer.Dispose();
                        _timer = null;
                    }
                }
                _disposed = true;
            }
        }

        ~BatteryService() => Dispose(false);
    }
} 