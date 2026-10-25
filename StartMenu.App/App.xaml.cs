using Microsoft.UI.Xaml;

namespace StartMenu_App;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new StartMenuWindow();
        _window.Activate();
    }
}
