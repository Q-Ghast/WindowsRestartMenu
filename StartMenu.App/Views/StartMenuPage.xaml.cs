using System;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using StartMenu.Core.Models;
using StartMenu.Core.Services;
using StartMenu_App.ViewModels;
using Windows.System;

namespace StartMenu_App.Views;

public sealed partial class StartMenuPage : UserControl
{
    public StartMenuPage()
    {
        var catalog = new FakeAppCatalog();
        ViewModel = new StartMenuViewModel(
            catalog,
            new JsonSettingsService(),
            new AppSearchService(catalog.AllApps));

        InitializeComponent();
        DataContext = ViewModel;
        ViewModel.CloseRequested += OnViewModelCloseRequested;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    public event EventHandler? RequestClose;

    public StartMenuViewModel ViewModel { get; }

    public bool HandleEscape()
    {
        if (ViewModel.IsSearchActive)
        {
            ClearSearch();
            return true;
        }

        if (ViewModel.IsCategoryDetailOpen)
        {
            ViewModel.GoBack();
            return true;
        }

        return false;
    }

    private void ClearSearch()
    {
        ViewModel.ClearSearch();
        SearchBox.Text = string.Empty;
        SearchBox.Focus(FocusState.Programmatic);
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (SearchBox.Text != ViewModel.SearchQuery)
        {
            ViewModel.SearchQuery = SearchBox.Text;
        }
    }

    private void OnSearchBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Down && ViewModel.SearchResults.Count > 0)
        {
            if (SearchResultsRepeater.TryGetElement(0) is Control first)
            {
                first.Focus(FocusState.Programmatic);
                e.Handled = true;
            }
        }
    }

    private void OnClearSearchClick(object sender, RoutedEventArgs e)
    {
        ClearSearch();
    }

    private void OnFindInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        SearchBox.Focus(FocusState.Programmatic);
        SearchBox.SelectAll();
    }

    private void OnViewModeClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: string tag }
            && Enum.TryParse<StartMenuViewMode>(tag, out var mode))
        {
            ViewModel.SetViewMode(mode);
        }
    }

    private void OnToggleCompactClick(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleCompact();
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        ViewModel.GoBack();
    }

    private void OnAccountActionClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item)
        {
            ViewModel.LogAccountAction(item.Text);
        }
    }

    private void OnPowerActionClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item)
        {
            ViewModel.LogPowerAction(item.Text);
        }
    }

    private void OnPageKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape && HandleEscape())
        {
            e.Handled = true;
        }
    }

    private void OnViewModelCloseRequested(object? sender, EventArgs e)
    {
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(StartMenuViewModel.IsSearchActive)
            or nameof(StartMenuViewModel.IsCategoryDetailOpen))
        {
            DispatcherQueue.TryEnqueue(() => ContentScrollViewer.ChangeView(null, 0, null, true));
        }

        if (e.PropertyName == nameof(StartMenuViewModel.IsCategoryDetailOpen)
            && ViewModel.IsCategoryDetailOpen)
        {
            DispatcherQueue.TryEnqueue(() => BackButton.Focus(FocusState.Programmatic));
        }
    }
}
