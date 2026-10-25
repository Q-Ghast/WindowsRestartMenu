namespace StartMenu.Core.Models;

public sealed record AppEntry(
    string Id,
    string DisplayName,
    string CategoryId,
    string Glyph,
    string AccentHex);
