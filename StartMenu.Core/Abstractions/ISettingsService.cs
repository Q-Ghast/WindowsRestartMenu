using StartMenu.Core.Models;

namespace StartMenu.Core.Abstractions;

public interface ISettingsService
{
    StartMenuSettings Load();

    void Save(StartMenuSettings settings);
}
