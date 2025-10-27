using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using QRTracker.Models;

namespace QRTracker.Services;

public class SharePointService
{
    private AppSettings? _settings;
    private string? _accessToken;

    public void Configure(AppSettings settings, string? accessToken)
    {
        _settings = settings;
        _accessToken = accessToken;
    }

    public bool IsConfigured => _settings?.UseSharePoint == true &&
                                !string.IsNullOrWhiteSpace(_settings.SiteId) &&
                                !string.IsNullOrWhiteSpace(_settings.DriveId) &&
                                !string.IsNullOrWhiteSpace(_settings.ItemId) &&
                                !string.IsNullOrWhiteSpace(_accessToken);

    public async Task<bool> AppendExcelRowAsync(SessionRecord record)
    {
        if (!IsConfigured) return false;
        var s = _settings!;

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        var url = $"https://graph.microsoft.com/v1.0/sites/{s.SiteId}/drives/{s.DriveId}/items/{s.ItemId}/workbook/tables/{s.TableName}/rows/add";

        var row = new object[]
        {
            record.StartUtc.UtcDateTime.ToString("o"),
            (int)record.Duration.TotalSeconds,
            record.ActionCode,
            record.Note ?? string.Empty,
            record.StationCode,
            record.DeviceCode
        };

        var payload = new { values = new List<object[]> { row } };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var resp = await http.PostAsync(url, content);
        return resp.IsSuccessStatusCode;
    }
}

