using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using StartMenu.Core.Models;
using StartMenu_App.Common;

namespace StartMenu_App.ViewModels;

public sealed class CategoryCardViewModel
{
    public CategoryCardViewModel(
        CategoryGroup group,
        IReadOnlyList<AppTileViewModel> apps,
        Action<CategoryCardViewModel>? open = null)
    {
        Group = group;
        Id = group.Id;
        DisplayName = group.DisplayName;
        Apps = apps;
        AppCount = apps.Count;
        Top1 = apps.ElementAtOrDefault(0);
        Top2 = apps.ElementAtOrDefault(1);
        Top3 = apps.ElementAtOrDefault(2);
        Top4 = apps.ElementAtOrDefault(3);
        OpenCommand = new RelayCommand(() => open?.Invoke(this));
    }

    public CategoryGroup Group { get; }

    public string Id { get; }

    public string DisplayName { get; }

    public IReadOnlyList<AppTileViewModel> Apps { get; }

    public int AppCount { get; }

    public AppTileViewModel? Top1 { get; }

    public AppTileViewModel? Top2 { get; }

    public AppTileViewModel? Top3 { get; }

    public AppTileViewModel? Top4 { get; }

    public ICommand OpenCommand { get; }
}
