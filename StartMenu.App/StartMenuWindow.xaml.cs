using System;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using StartMenu_App.Interop;
using StartMenu_App.Services;
using Windows.UI;
using Windows.UI.ViewManagement;
using WinRT.Interop;

namespace StartMenu_App;

public sealed partial class StartMenuWindow : Window
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwaBorderColor = 34;
    private const int DwmwcpRound = 2;
    private const int DwmwcpDoNotRound = 1;
    private const int DwmColorNone = unchecked((int)0xFFFFFFFE);
    private const int GwlExStyle = -20;
    private const int WsExToolWindow = 0x00000080;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;

    private readonly bool _noAutoClose;
    private DispatcherQueueTimer? _startupGraceTimer;
    private DispatcherQueueTimer? _autoCloseTimer;
    private bool _autoCloseArmed;
    private bool _isClosing;

    public StartMenuWindow()
    {
        InitializeComponent();

        Title = "StartMenu";
        _noAutoClose = HasArg("--no-auto-close");

        ConfigurePresenter();
        ConfigureBackdrop();

        Activated += OnActivated;
        Closed += OnClosed;
        RootGrid.Loaded += OnRootGridLoaded;
        RootGrid.ActualThemeChanged += OnActualThemeChanged;
        MenuPage.RequestClose += OnPageRequestClose;

        StartStartupGrace();
    }

    private static bool HasArg(string arg)
    {
        return Array.Exists(
            Environment.GetCommandLineArgs(),
            value => string.Equals(value, arg, StringComparison.OrdinalIgnoreCase));
    }

    private void ConfigurePresenter()
    {
        AppWindow.IsShownInSwitchers = false;
        ExtendsContentIntoTitleBar = true;

        var presenter = AppWindow.Presenter as OverlappedPresenter;
        if (presenter is null)
        {
            presenter = OverlappedPresenter.Create();
            AppWindow.SetPresenter(presenter);
        }

        presenter.SetBorderAndTitleBar(false, false);
        presenter.IsResizable = false;
        presenter.IsMaximizable = false;
        presenter.IsMinimizable = false;
        presenter.IsAlwaysOnTop = true;
    }

    private void ConfigureBackdrop()
    {
        if (DesktopAcrylicController.IsSupported())
        {
            return;
        }

        RootGrid.Background = new SolidColorBrush(Color.FromArgb(255, 32, 32, 32));
        PanelBorder.Background = new SolidColorBrush(Color.FromArgb(255, 32, 32, 32));
    }

    private void OnRootGridLoaded(object sender, RoutedEventArgs e)
    {
        ApplyDwmAttributes();
        PlaceWindow();
    }

    private void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        ApplyDwmAttributes();
    }

    private void ApplyDwmAttributes()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var cornerPreference = _isClosing ? DwmwcpDoNotRound : DwmwcpRound;
        _ = NativeMethods.DwmSetWindowAttribute(hwnd, DwmwaWindowCornerPreference, ref cornerPreference, sizeof(int));

        var borderColor = DwmColorNone;
        _ = NativeMethods.DwmSetWindowAttribute(hwnd, DwmwaBorderColor, ref borderColor, sizeof(int));

        var useDarkMode = RootGrid.ActualTheme == ElementTheme.Dark ? 1 : 0;
        _ = NativeMethods.DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref useDarkMode, sizeof(int));

        var extendedStyle = NativeMethods.GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
        extendedStyle |= WsExToolWindow;
        _ = NativeMethods.SetWindowLongPtr(hwnd, GwlExStyle, new IntPtr(extendedStyle));
        _ = NativeMethods.SetWindowPos(
            hwnd,
            IntPtr.Zero,
            0,
            0,
            0,
            0,
            SwpNoSize | SwpNoMove | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
    }

    private void PlaceWindow()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        WindowPlacementService.PlaceBottomCenter(hwnd, AppWindow);
    }

    private void StartStartupGrace()
    {
        _startupGraceTimer = DispatcherQueue.CreateTimer();
        _startupGraceTimer.Interval = TimeSpan.FromMilliseconds(600);
        _startupGraceTimer.IsRepeating = false;
        _startupGraceTimer.Tick += (_, _) =>
        {
            _startupGraceTimer?.Stop();
            _startupGraceTimer = null;
            _autoCloseArmed = true;
        };
        _startupGraceTimer.Start();
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState != WindowActivationState.Deactivated)
        {
            return;
        }

        if (_noAutoClose || !_autoCloseArmed || _isClosing)
        {
            return;
        }

        _autoCloseTimer?.Stop();
        _autoCloseTimer = DispatcherQueue.CreateTimer();
        _autoCloseTimer.Interval = TimeSpan.FromMilliseconds(150);
        _autoCloseTimer.IsRepeating = false;
        _autoCloseTimer.Tick += (_, _) =>
        {
            _autoCloseTimer?.Stop();
            _autoCloseTimer = null;
            BeginClose();
        };
        _autoCloseTimer.Start();
    }

    private void OnEscapeInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        if (MenuPage.HandleEscape())
        {
            return;
        }

        BeginClose();
    }

    private void OnPageRequestClose(object? sender, EventArgs args)
    {
        BeginClose();
    }

    private void BeginClose()
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;

        if (!new UISettings().AnimationsEnabled)
        {
            Close();
            return;
        }

        var animation = new DoubleAnimation
        {
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(100)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        Storyboard.SetTarget(animation, RootGrid);
        Storyboard.SetTargetProperty(animation, "Opacity");

        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        storyboard.Completed += (_, _) => Close();
        storyboard.Begin();
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        _startupGraceTimer?.Stop();
        _autoCloseTimer?.Stop();
        _startupGraceTimer = null;
        _autoCloseTimer = null;

        Activated -= OnActivated;
        Closed -= OnClosed;
        RootGrid.Loaded -= OnRootGridLoaded;
        RootGrid.ActualThemeChanged -= OnActualThemeChanged;
        MenuPage.RequestClose -= OnPageRequestClose;
        SystemBackdrop = null;
    }
}
