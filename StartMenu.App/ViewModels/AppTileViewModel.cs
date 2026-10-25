using System;
using System.Globalization;
using Microsoft.UI.Xaml.Media;
using StartMenu.Core.Models;
using Windows.UI;

namespace StartMenu_App.ViewModels;

public sealed class AppTileViewModel
{
    public AppTileViewModel(AppEntry app)
    {
        Id = app.Id;
        DisplayName = app.DisplayName;
        Glyph = app.Glyph;
        AccentBrush = new SolidColorBrush(ParseColor(app.AccentHex));
    }

    public string Id { get; }

    public string DisplayName { get; }

    public string Glyph { get; }

    public Brush AccentBrush { get; }

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
