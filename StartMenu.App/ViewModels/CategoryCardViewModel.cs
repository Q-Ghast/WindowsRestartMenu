using System.Collections.Generic;
using System.Linq;
using StartMenu.Core.Models;

namespace StartMenu_App.ViewModels;

public sealed class CategoryCardViewModel
{
    public CategoryCardViewModel(CategoryGroup group, IReadOnlyList<AppTileViewModel> apps)
    {
        Id = group.Id;
        DisplayName = group.DisplayName;
        AppCount = apps.Count;
        Top1 = apps.ElementAtOrDefault(0);
        Top2 = apps.ElementAtOrDefault(1);
        Top3 = apps.ElementAtOrDefault(2);
        Top4 = apps.ElementAtOrDefault(3);
    }

    public string Id { get; }

    public string DisplayName { get; }

    public int AppCount { get; }

    public AppTileViewModel? Top1 { get; }

    public AppTileViewModel? Top2 { get; }

    public AppTileViewModel? Top3 { get; }

    public AppTileViewModel? Top4 { get; }
}
