using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace StartMenu_App.Controls;

/// <summary>
/// A neutral, thin acrylic backdrop for the rounded panel. Kept on
/// SystemBackdropElement so the acrylic is clipped to the panel corners.
/// </summary>
public sealed class TintedAcrylicBackdrop : SystemBackdrop
{
    private DesktopAcrylicController? _controller;
    private SystemBackdropConfiguration? _configuration;

    protected override void OnTargetConnected(
        ICompositionSupportsSystemBackdrop connectedTarget,
        XamlRoot xamlRoot)
    {
        base.OnTargetConnected(connectedTarget, xamlRoot);

        if (!DesktopAcrylicController.IsSupported())
        {
            return;
        }

        var dark = (xamlRoot.Content as FrameworkElement)?.ActualTheme != ElementTheme.Light;

        _configuration = new SystemBackdropConfiguration
        {
            IsInputActive = true,
            Theme = dark ? SystemBackdropTheme.Dark : SystemBackdropTheme.Light
        };

        _controller = new DesktopAcrylicController
        {
            Kind = DesktopAcrylicKind.Thin
        };

        ApplyTint(dark);
        _controller.AddSystemBackdropTarget(connectedTarget);
        _controller.SetSystemBackdropConfiguration(_configuration);
    }

    public void UpdateTheme(ElementTheme theme)
    {
        ApplyTint(theme != ElementTheme.Light);
    }

    private void ApplyTint(bool dark)
    {
        if (_configuration is not null)
        {
            _configuration.Theme = dark
                ? SystemBackdropTheme.Dark
                : SystemBackdropTheme.Light;
        }

        if (_controller is null)
        {
            return;
        }

        _controller.TintColor = dark
            ? Color.FromArgb(255, 22, 22, 22)
            : Color.FromArgb(255, 246, 246, 246);
        _controller.TintOpacity = dark ? 0.24f : 0.20f;
        _controller.LuminosityOpacity = dark ? 0.30f : 0.26f;
        _controller.FallbackColor = dark
            ? Color.FromArgb(255, 28, 28, 28)
            : Color.FromArgb(255, 243, 243, 243);
    }

    protected override void OnTargetDisconnected(
        ICompositionSupportsSystemBackdrop disconnectedTarget)
    {
        _controller?.Dispose();
        _controller = null;
        _configuration = null;

        base.OnTargetDisconnected(disconnectedTarget);
    }
}
