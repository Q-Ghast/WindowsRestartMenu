using Microsoft.UI.Xaml.Controls;
using StartMenu.Core.Services;
using StartMenu_App.ViewModels;

namespace StartMenu_App.Views;

public sealed partial class StartMenuPage : UserControl
{
    public StartMenuPage()
    {
        InitializeComponent();
    }

    public StartMenuViewModel ViewModel { get; } = new(new FakeAppCatalog());
}
