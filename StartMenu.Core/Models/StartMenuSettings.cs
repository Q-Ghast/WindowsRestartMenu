namespace StartMenu.Core.Models;

public sealed class StartMenuSettings
{
    public int SchemaVersion { get; set; } = 1;

    public StartMenuViewMode ViewMode { get; set; } = StartMenuViewMode.Category;

    public bool CompactPins { get; set; }
}
