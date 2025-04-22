using System.Windows;
using BabyEngine.Models; // Needed for LicenseService

namespace BabyEngine.Views
{
    /// <summary>
    /// Interaction logic for LicenseWindow.xaml
    /// </summary>
    public partial class LicenseWindow : Window
    {
        public LicenseService.LicenseInfo? ValidatedLicenseInfo { get; private set; }
        private readonly ConfigService _configService;

        public LicenseWindow(ConfigService configService)
        {
            InitializeComponent();
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            Owner = Application.Current.MainWindow; // Set owner to center on main window if visible
        }

        private void ActivateButton_Click(object sender, RoutedEventArgs e)
        {
            string licenseKey = LicenseKeyTextBox.Text.Trim();
            ErrorTextBlock.Text = string.Empty; // Clear previous errors

            if (string.IsNullOrWhiteSpace(licenseKey))
            {
                ErrorTextBlock.Text = "Please enter a license key.";
                return;
            }

            var licenseInfo = LicenseService.ValidateLicenseKey(licenseKey);

            if (licenseInfo.IsValid)
            {
                ValidatedLicenseInfo = licenseInfo;
                // Save the valid key
                _configService.SetLicenseKey(licenseKey); 
                DialogResult = true; // Signals success to the caller
                Close();
            }
            else
            {
                ErrorTextBlock.Text = licenseInfo.ErrorMessage;
                 // Optionally shake the window or give other visual feedback
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // Signals cancellation
            Close();
        }
    }
} 