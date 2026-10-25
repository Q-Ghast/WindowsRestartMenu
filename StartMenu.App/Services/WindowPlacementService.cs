using System;
using Microsoft.UI.Windowing;
using StartMenu_App.Interop;
using Windows.Graphics;

namespace StartMenu_App.Services;

internal static class WindowPlacementService
{
    private const double MenuWidth = 888;
    private const double MenuHeight = 832;
    private const double HorizontalMargin = 24;
    private const double VerticalMargin = 16;
    private const double TaskbarGap = 8;

    public static void PlaceBottomCenter(IntPtr hwnd, AppWindow appWindow)
    {
        var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
        if (displayArea is null)
        {
            return;
        }

        var workArea = displayArea.WorkArea;
        var scale = NativeMethods.GetDpiForWindow(hwnd) / 96.0;
        if (scale <= 0)
        {
            scale = 1.0;
        }

        var width = Math.Max(
            320 * scale,
            Math.Min(MenuWidth * scale, workArea.Width - HorizontalMargin * scale));
        var height = Math.Max(
            320 * scale,
            Math.Min(MenuHeight * scale, workArea.Height - VerticalMargin * scale));

        var x = workArea.X + (int)Math.Round((workArea.Width - width) / 2.0);
        var y = workArea.Y + workArea.Height - (int)Math.Round(height) - (int)Math.Round(TaskbarGap * scale);

        appWindow.MoveAndResize(new RectInt32(x, y, (int)Math.Round(width), (int)Math.Round(height)));
    }
}
