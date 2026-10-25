using System.Collections.Generic;

namespace StartMenu_App.ViewModels;

public sealed class AppLetterGroupViewModel
{
    public AppLetterGroupViewModel(string header, IReadOnlyList<AppTileViewModel> apps)
    {
        Header = header;
        Apps = apps;
    }

    public string Header { get; }

    public IReadOnlyList<AppTileViewModel> Apps { get; }
}
