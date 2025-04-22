using System.Windows;

namespace BabyEngine;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Directly create and show the main window.
        // The license check will now happen inside MainWindow's OnLoaded event.
        var mainWindow = new MainWindow(); 
        mainWindow.Show();
    }
}

