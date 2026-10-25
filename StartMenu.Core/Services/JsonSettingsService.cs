using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using StartMenu.Core.Abstractions;
using StartMenu.Core.Models;

namespace StartMenu.Core.Services;

public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path;

    public JsonSettingsService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowsReStartMenu",
            "settings.json"))
    {
    }

    public JsonSettingsService(string path)
    {
        _path = path;
    }

    public StartMenuSettings Load()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return new StartMenuSettings();
            }

            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<StartMenuSettings>(json, SerializerOptions)
                ?? new StartMenuSettings();
        }
        catch
        {
            return new StartMenuSettings();
        }
    }

    public void Save(StartMenuSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempPath = _path + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(settings, SerializerOptions));
            File.Move(tempPath, _path, overwrite: true);
        }
        catch
        {
            // Settings are best-effort; a read-only profile must not break the menu.
        }
    }
}
