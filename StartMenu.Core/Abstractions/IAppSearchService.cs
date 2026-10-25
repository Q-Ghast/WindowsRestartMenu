using StartMenu.Core.Models;

namespace StartMenu.Core.Abstractions;

public interface IAppSearchService
{
    IReadOnlyList<AppEntry> Search(string query, int maxResults);
}
