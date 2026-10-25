using System.Collections.Generic;
using System.Linq;
using StartMenu.Core.Services;

namespace StartMenu_App.ViewModels;

public sealed class StartMenuViewModel
{
    public StartMenuViewModel(FakeAppCatalog catalog)
    {
        var tiles = catalog.AllApps.ToDictionary(app => app.Id, app => new AppTileViewModel(app));

        PinnedApps = catalog.PinnedApps
            .Select(app => tiles[app.Id])
            .ToArray();

        Categories = catalog.Categories
            .Select(group => new CategoryCardViewModel(
                group,
                group.Apps.Select(app => tiles[app.Id]).ToArray()))
            .ToArray();
    }

    public IReadOnlyList<AppTileViewModel> PinnedApps { get; }

    public IReadOnlyList<CategoryCardViewModel> Categories { get; }

    public string UserName { get; } = "Xianene Q";

    public string UserInitial { get; } = "X";
}
