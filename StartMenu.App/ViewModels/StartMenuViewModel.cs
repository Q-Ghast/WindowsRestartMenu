using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.UI.Xaml;
using StartMenu.Core.Abstractions;
using StartMenu.Core.Models;
using StartMenu.Core.Services;
using StartMenu_App.Common;

namespace StartMenu_App.ViewModels;

public sealed class StartMenuViewModel : ObservableObject
{
    private readonly IAppSearchService _searchService;
    private readonly ISettingsService _settingsService;
    private readonly StartMenuSettings _settings;

    private string _searchQuery = string.Empty;
    private bool _isSearchActive;
    private StartMenuViewMode _viewMode;
    private bool _compactPins;
    private bool _isCategoryDetailOpen;
    private CategoryCardViewModel? _selectedCategory;

    public StartMenuViewModel(
        FakeAppCatalog catalog,
        ISettingsService settingsService,
        IAppSearchService searchService)
    {
        _settingsService = settingsService;
        _searchService = searchService;
        _settings = settingsService.Load();
        _viewMode = _settings.ViewMode;
        _compactPins = _settings.CompactPins;

        AllApps = catalog.AllApps
            .Select(app => new AppTileViewModel(app, ActivateApp))
            .ToArray();

        var byId = AllApps.ToDictionary(tile => tile.Id);
        PinnedApps = catalog.PinnedApps
            .Select(app => byId[app.Id])
            .ToArray();

        Categories = catalog.Categories
            .Select(group => new CategoryCardViewModel(
                group,
                group.Apps.Select(app => byId[app.Id]).ToArray(),
                OpenCategory))
            .ToArray();

        AllAppsGroups = BuildGroups(AllApps);
        SearchResults = new ObservableCollection<AppTileViewModel>();
    }

    public event EventHandler? CloseRequested;

    public IReadOnlyList<AppTileViewModel> AllApps { get; }

    public IReadOnlyList<AppLetterGroupViewModel> AllAppsGroups { get; }

    public IReadOnlyList<AppTileViewModel> PinnedApps { get; }

    public IReadOnlyList<CategoryCardViewModel> Categories { get; }

    public ObservableCollection<AppTileViewModel> SearchResults { get; }

    public string UserName { get; } = "Xianene Q";

    public string UserInitial { get; } = "X";

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value ?? string.Empty))
            {
                RefreshSearch();
            }
        }
    }

    public bool IsSearchActive => _isSearchActive;

    public StartMenuViewMode ViewMode => _viewMode;

    public bool CompactPins => _compactPins;

    public bool IsCategoryDetailOpen => _isCategoryDetailOpen;

    public string SelectedCategoryName => _selectedCategory?.DisplayName ?? string.Empty;

    public IReadOnlyList<AppTileViewModel> SelectedCategoryApps =>
        _selectedCategory?.Apps ?? Array.Empty<AppTileViewModel>();

    public string ViewModeLabel => _viewMode switch
    {
        StartMenuViewMode.Grid => "视图: 网格",
        StartMenuViewMode.List => "视图: 列表",
        _ => "视图: 类别"
    };

    public string CompactLabel => _compactPins ? "显示详细信息" : "显示精简信息";

    public string CompactChevronGlyph => _compactPins ? "\uE70D" : "\uE70E";

    public Visibility HomeVisibility =>
        !_isSearchActive && !_isCategoryDetailOpen ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SearchVisibility =>
        _isSearchActive && !_isCategoryDetailOpen ? Visibility.Visible : Visibility.Collapsed;

    public Visibility CategoryDetailVisibility =>
        _isCategoryDetailOpen ? Visibility.Visible : Visibility.Collapsed;

    public Visibility SearchEmptyVisibility =>
        _isSearchActive && SearchResults.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ClearSearchVisibility =>
        _isSearchActive ? Visibility.Visible : Visibility.Collapsed;

    public Visibility NormalPinsVisibility =>
        _compactPins ? Visibility.Collapsed : Visibility.Visible;

    public Visibility CompactPinsVisibility =>
        _compactPins ? Visibility.Visible : Visibility.Collapsed;

    public Visibility CategoryViewVisibility =>
        _viewMode == StartMenuViewMode.Category ? Visibility.Visible : Visibility.Collapsed;

    public Visibility GridViewVisibility =>
        _viewMode == StartMenuViewMode.Grid ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ListViewVisibility =>
        _viewMode == StartMenuViewMode.List ? Visibility.Visible : Visibility.Collapsed;

    public void ClearSearch()
    {
        SearchQuery = string.Empty;
    }

    public bool GoBack()
    {
        if (_isCategoryDetailOpen)
        {
            _isCategoryDetailOpen = false;
            _selectedCategory = null;
            OnPropertyChanged(nameof(IsCategoryDetailOpen));
            OnPropertyChanged(nameof(SelectedCategoryName));
            OnPropertyChanged(nameof(SelectedCategoryApps));
            RaiseVisibilityChanged();
            return true;
        }

        if (_isSearchActive)
        {
            ClearSearch();
            return true;
        }

        return false;
    }

    public void SetViewMode(StartMenuViewMode mode)
    {
        if (_viewMode == mode)
        {
            return;
        }

        _viewMode = mode;
        _settings.ViewMode = mode;
        SaveSettings();

        OnPropertyChanged(nameof(ViewMode));
        OnPropertyChanged(nameof(ViewModeLabel));
        OnPropertyChanged(nameof(CategoryViewVisibility));
        OnPropertyChanged(nameof(GridViewVisibility));
        OnPropertyChanged(nameof(ListViewVisibility));
    }

    public void ToggleCompact()
    {
        _compactPins = !_compactPins;
        _settings.CompactPins = _compactPins;
        SaveSettings();

        OnPropertyChanged(nameof(CompactPins));
        OnPropertyChanged(nameof(CompactLabel));
        OnPropertyChanged(nameof(CompactChevronGlyph));
        OnPropertyChanged(nameof(NormalPinsVisibility));
        OnPropertyChanged(nameof(CompactPinsVisibility));
    }

    public void LogAccountAction(string action)
    {
        Debug.WriteLine($"Mock account action: {action}");
    }

    public void LogPowerAction(string action)
    {
        Debug.WriteLine($"Mock power action: {action}");
    }

    private void ActivateApp(AppTileViewModel app)
    {
        Debug.WriteLine($"Mock launch: {app.DisplayName}");
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OpenCategory(CategoryCardViewModel category)
    {
        _selectedCategory = category;
        _isCategoryDetailOpen = true;
        SearchQuery = string.Empty;

        OnPropertyChanged(nameof(IsCategoryDetailOpen));
        OnPropertyChanged(nameof(SelectedCategoryName));
        OnPropertyChanged(nameof(SelectedCategoryApps));
        RaiseVisibilityChanged();
    }

    private void RefreshSearch()
    {
        SearchResults.Clear();
        if (!string.IsNullOrWhiteSpace(_searchQuery))
        {
            foreach (var app in _searchService.Search(_searchQuery, 8))
            {
                SearchResults.Add(AllApps.First(tile => tile.Id == app.Id));
            }
        }

        var active = !string.IsNullOrWhiteSpace(_searchQuery);
        if (_isSearchActive != active)
        {
            _isSearchActive = active;
            OnPropertyChanged(nameof(IsSearchActive));
        }

        RaiseVisibilityChanged();
    }

    private void RaiseVisibilityChanged()
    {
        OnPropertyChanged(nameof(HomeVisibility));
        OnPropertyChanged(nameof(SearchVisibility));
        OnPropertyChanged(nameof(CategoryDetailVisibility));
        OnPropertyChanged(nameof(SearchEmptyVisibility));
        OnPropertyChanged(nameof(ClearSearchVisibility));
    }

    private void SaveSettings()
    {
        _settingsService.Save(_settings);
    }

    private static IReadOnlyList<AppLetterGroupViewModel> BuildGroups(
        IEnumerable<AppTileViewModel> apps)
    {
        return apps
            .OrderBy(app => app.DisplayName, StringComparer.CurrentCulture)
            .GroupBy(app => GetGroupHeader(app.DisplayName))
            .Select(group => new AppLetterGroupViewModel(group.Key, group.ToArray()))
            .ToArray();
    }

    private static string GetGroupHeader(string displayName)
    {
        if (string.IsNullOrEmpty(displayName))
        {
            return "#";
        }

        var first = displayName[0];
        return first is >= 'A' and <= 'Z' or >= 'a' and <= 'z'
            ? char.ToUpperInvariant(first).ToString()
            : "#";
    }
}
