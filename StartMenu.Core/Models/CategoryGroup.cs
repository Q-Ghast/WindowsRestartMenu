namespace StartMenu.Core.Models;

public sealed record CategoryGroup(
    string Id,
    string DisplayName,
    IReadOnlyList<AppEntry> Apps);
