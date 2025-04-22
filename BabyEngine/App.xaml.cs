using System.Threading; // Added for Mutex
using System.Windows;

namespace BabyEngine;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static Mutex? _mutex = null;

    protected override void OnStartup(StartupEventArgs e)
    {
        const string appName = "BabyEngineAppMutex";
        bool createdNew;

        _mutex = new Mutex(true, appName, out createdNew);

        if (!createdNew)
        {
            // App is already running! Exiting the application
            MessageBox.Show("Mommy is already running, little one! Look for the other window.", "BabyEngine Already Running", MessageBoxButton.OK, MessageBoxImage.Information);
            Application.Current.Shutdown();
            return; // Important to exit the method here
        }

        base.OnStartup(e);

        // Directly create and show the main window.
        // The license check will now happen inside MainWindow's OnLoaded event.
        var mainWindow = new MainWindow(); 
        mainWindow.Show();
    }
}

