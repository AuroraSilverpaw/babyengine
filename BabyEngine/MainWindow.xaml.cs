using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BabyEngine.ViewModels;
using BabyEngine.Models;
using BabyEngine.Views; // Needed for LicenseWindow

namespace BabyEngine;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private static bool _isAlreadyOpen = false; // Flag to prevent multiple instances

    private ConfigService? _configService;
    private LicenseService.LicenseInfo? _validatedLicenseInfo;
    private ChatViewModel? _viewModel;

    public MainWindow()
    {
        // Check if an instance is already open
        if (_isAlreadyOpen)
        {
            // Close this instance immediately if another is already initializing
            this.Close(); 
            return; 
        }
        _isAlreadyOpen = true; // Mark this instance as open
        
        InitializeComponent();
        this.Loaded += MainWindow_Loaded;
        this.Closed += MainWindow_Closed;
        this.Closed += (s, e) => _isAlreadyOpen = false; // Reset flag AFTER saving attempts
        KeyDown += MainWindow_KeyDown;
        MouseLeftButtonDown += MainWindow_MouseLeftButtonDown;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _configService = new ConfigService();
        string storedLicenseKey = _configService.GetLicenseKey();
        bool licenseOk = false;

        if (!string.IsNullOrWhiteSpace(storedLicenseKey))
        {
            _validatedLicenseInfo = LicenseService.ValidateLicenseKey(storedLicenseKey);
            if (_validatedLicenseInfo.IsValid)
            {
                licenseOk = true;
            }
            else
            {
                MessageBox.Show(this, $"Your saved license is invalid: {_validatedLicenseInfo.ErrorMessage}\nPlease enter a valid license key.", 
                                "License Invalid", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        
        if (!licenseOk)
        {
            if (PromptForLicense(_configService)) {
                licenseOk = true;
            }
        }

        if (!licenseOk || _validatedLicenseInfo == null || !_validatedLicenseInfo.IsValid)
        {   
            MessageBox.Show(this, "A valid license key is required to use BabyEngine. The application will now close.", 
                            "Activation Required", MessageBoxButton.OK, MessageBoxImage.Error);
            this.Close();
            return;
        }

        try
        {
            // Load the saved application state
            AppState loadedState = _configService.LoadAppState();
            
            // Initialize ViewModel with license info and loaded state
            _viewModel = new ChatViewModel(_validatedLicenseInfo, loadedState);
            DataContext = _viewModel;

            SetupViewModelBindings();

            InitializeSettingsPopup();
        }
        catch (Exception ex) 
        {
            MessageBox.Show(this, $"An error occurred during initialization: {ex.Message}", 
                            "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            this.Close();
        }
    }
    
    private bool PromptForLicense(ConfigService configService)
    {
        var licenseWindow = new LicenseWindow(configService) { Owner = this };
        bool? dialogResult = licenseWindow.ShowDialog(); 

        if (dialogResult == true && licenseWindow.ValidatedLicenseInfo != null)
        {
            _validatedLicenseInfo = licenseWindow.ValidatedLicenseInfo;
            return true;
        }
        else
        {
            _validatedLicenseInfo = null;
            return false;
        }
    }

    private void SetupViewModelBindings()
    {
        if (_viewModel == null) return;

        _viewModel.Messages.CollectionChanged += (s, e) =>
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                MessagesScrollViewer.ScrollToEnd();
            }
        };
    }

    private void InitializeSettingsPopup()
    {
        SettingsPopup.Opened += (s, e) =>
        {
            if (_viewModel != null)
            {
                BlushyFrequencySlider.Value = _viewModel.BlushyMessagesPerHour;
            }
        };
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }
    
    private void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }
    
    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null && _viewModel.SendMessageCommand.CanExecute(null))
        {
            _viewModel.SendMessageCommand.Execute(null);
        }
    }
    
    private void TextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
        {
            e.Handled = true;
            if (_viewModel != null && _viewModel.SendMessageCommand.CanExecute(null))
            {
                _viewModel.SendMessageCommand.Execute(null);
            }
        }
    }
    
    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null && _viewModel.TogglePauseCommand.CanExecute(null))
        {
            _viewModel.TogglePauseCommand.Execute(null);
        }
    }
    
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
    
    private void MenuButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsPopup.IsOpen = true;
    }
    
    private void CloseSettings_Click(object sender, RoutedEventArgs e)
    {
        SettingsPopup.IsOpen = false;
    }
    
    private void ChangeLicenseKey_Click(object sender, RoutedEventArgs e)
    {
        SettingsPopup.IsOpen = false;

        if (_configService == null) { 
            MessageBox.Show(this, "Internal Error: Config Service not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        bool licenseChanged = PromptForLicense(_configService);

        if (licenseChanged)
        {
            MessageBox.Show(this, "License key updated successfully! Please restart BabyEngine for the changes to take effect.", 
                            "Restart Required", MessageBoxButton.OK, MessageBoxImage.Information);
            
            _viewModel?.RefreshLicenseStatus();
        }
        else if (_validatedLicenseInfo == null)
        {
            MessageBox.Show(this, "Failed to update license key. The entered key might be invalid.", "License Update Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
    
    private void ShowFeatures_Click(object sender, RoutedEventArgs e)
    {
        FeaturesPopup.IsOpen = !FeaturesPopup.IsOpen;
        if (FeaturesPopup.IsOpen && ReminderDatePicker != null && ReminderDatePicker.SelectedDate == null) {
            ReminderDatePicker.SelectedDate = DateTime.Now.Date.AddHours(9);
        }
    }
    
    private void AddReminder_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null && ReminderTitleInput != null && ReminderDatePicker != null && 
            !string.IsNullOrWhiteSpace(ReminderTitleInput.Text) && ReminderDatePicker.SelectedDate.HasValue)
        {
            string title = ReminderTitleInput.Text;
            string message = "Your reminder is due!";
            DateTime dueTime = ReminderDatePicker.SelectedDate.Value;
            
            if (dueTime.TimeOfDay == TimeSpan.Zero)
            {
                dueTime = dueTime.Date.AddHours(9);
            }
            
            string parameter = $"{title}|{message}|{dueTime:o}";

            if (_viewModel.AddReminderCommand.CanExecute(parameter)) 
            {
                _viewModel.AddReminderCommand.Execute(parameter);
                
                ReminderTitleInput.Text = "";
                FeaturesPopup.IsOpen = false;
            }
             else {
                  MessageBox.Show(this, "Could not add reminder.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
             }
        }
         else {
              // Maybe provide visual feedback if fields are empty/invalid
         }
    }

    // Handler for the Closed event to save state
    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        Console.WriteLine("[DEBUG] MainWindow_Closed event triggered."); // Log entry
        if (_viewModel != null && _configService != null)
        {
            try
            {
                AppState currentState = _viewModel.GetCurrentState();
                Console.WriteLine("[DEBUG] Got current state from ViewModel. Calling SaveAppState..."); // Log before save
                _configService.SaveAppState(currentState);
                Console.WriteLine("[DEBUG] SaveAppState called successfully."); // Log after save
            }
            catch (Exception ex)
            {
                 Console.WriteLine($"[DEBUG] Error during state saving in MainWindow_Closed: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("[DEBUG] ViewModel or ConfigService was null in MainWindow_Closed. State not saved.");
        }
         _isAlreadyOpen = false; 
    }
}