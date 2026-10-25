using System;
using System.Globalization;
using System.IO;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using StartMenu.Core.Models;
using StartMenu_App.Common;
using Windows.UI;

namespace StartMenu_App.ViewModels;

public sealed class AppTileViewModel : ObservableObject
{
    private string _searchQuery = string.Empty;

    public AppTileViewModel(AppEntry app, Action<AppTileViewModel>? activate = null)
    {
        App = app;
        Id = app.Id;
        DisplayName = app.DisplayName;
        Glyph = app.Glyph;
        AccentBrush = new SolidColorBrush(ParseColor(app.AccentHex));
        ActivateCommand = new RelayCommand(() => activate?.Invoke(this));

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcons", app.Id + ".png");
        if (File.Exists(iconPath))
        {
            IconSource = new BitmapImage(new Uri(iconPath, UriKind.Absolute))
            {
                DecodePixelWidth = 96
            };
            IconVisibility = Visibility.Visible;
            FallbackVisibility = Visibility.Collapsed;
        }
        else
        {
            IconVisibility = Visibility.Collapsed;
            FallbackVisibility = Visibility.Visible;
        }
    }

    public AppEntry App { get; }

    public string Id { get; }

    public string SearchQuery
    {
        get => _searchQuery;
        set => SetProperty(ref _searchQuery, value ?? string.Empty);
    }

    public string DisplayName { get; }

    public string Glyph { get; }

    public Brush AccentBrush { get; }

    public ImageSource? IconSource { get; }

    public Visibility IconVisibility { get; }

    public Visibility FallbackVisibility { get; }

    public ICommand ActivateCommand { get; }

    private static Color ParseColor(string hex)
    {
        var value = hex.TrimStart('#');
        if (!uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            return Color.FromArgb(255, 77, 107, 254);
        }

        return Color.FromArgb(
            255,
            (byte)((rgb >> 16) & 0xFF),
            (byte)((rgb >> 8) & 0xFF),
            (byte)(rgb & 0xFF));
    }
}
