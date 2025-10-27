using System.Text.Json;
using QRTracker.Models;

namespace QRTracker.Services;

public class SettingsService
{
    private readonly string _path;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public SettingsService()
    {
        var dir = FileSystem.AppDataDirectory;
        _path = Path.Combine(dir, "settings.json");
    }

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(_path))
            return new AppSettings();

        await using var fs = File.OpenRead(_path);
        var settings = await JsonSerializer.DeserializeAsync<AppSettings>(fs, _jsonOptions);
        return settings ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        await using var fs = File.Create(_path);
        await JsonSerializer.SerializeAsync(fs, settings, _jsonOptions);
    }
}

