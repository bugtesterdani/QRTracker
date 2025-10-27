using System.Text.Json;
using QRTracker.Models;

namespace QRTracker.Services;

public class LocalDataService
{
    private readonly string _path;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public LocalDataService()
    {
        var dir = FileSystem.AppDataDirectory;
        _path = Path.Combine(dir, "history.json");
    }

    public async Task<List<SessionRecord>> LoadAsync()
    {
        if (!File.Exists(_path)) return new List<SessionRecord>();
        await using var fs = File.OpenRead(_path);
        var list = await JsonSerializer.DeserializeAsync<List<SessionRecord>>(fs, _jsonOptions);
        return list ?? new List<SessionRecord>();
    }

    public async Task AppendAsync(SessionRecord record)
    {
        var list = await LoadAsync();
        list.Add(record);
        await SaveAllAsync(list);
    }

    public async Task SaveAllAsync(List<SessionRecord> list)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        await using var fs = File.Create(_path);
        await JsonSerializer.SerializeAsync(fs, list, _jsonOptions);
    }
}

