using System;
using System.Collections.Generic;
using System.Linq;
using StartMenu.Core.Abstractions;
using StartMenu.Core.Models;

namespace StartMenu.Core.Services;

public sealed class AppSearchService : IAppSearchService
{
    private readonly IReadOnlyList<AppEntry> _apps;

    public AppSearchService(IReadOnlyList<AppEntry> apps)
    {
        _apps = apps;
    }

    public IReadOnlyList<AppEntry> Search(string query, int maxResults)
    {
        var normalized = query.Trim();
        if (normalized.Length == 0)
        {
            return Array.Empty<AppEntry>();
        }

        return _apps
            .Select(app => new { App = app, Score = Score(app, normalized) })
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.App.DisplayName, StringComparer.CurrentCulture)
            .Take(Math.Max(1, maxResults))
            .Select(item => item.App)
            .ToArray();
    }

    private static int Score(AppEntry app, string query)
    {
        var name = app.DisplayName;
        if (name.Equals(query, StringComparison.OrdinalIgnoreCase))
        {
            return 100;
        }

        if (name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 80;
        }

        if (name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Any(word => word.StartsWith(query, StringComparison.OrdinalIgnoreCase)))
        {
            return 60;
        }

        var initials = string.Concat(
            name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpperInvariant(word[0])));
        if (initials.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 50;
        }

        if (name.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return 40;
        }

        return 0;
    }
}
