using QRTracker.Models;
using System.Globalization;
using QRTracker.Services;
using QRTracker.Helpers;

namespace QRTracker.Pages;

public partial class HistoryPage : ContentPage
{
    private readonly LocalDataService _localData;
    private List<SessionRecord> _records = new();

    public HistoryPage()
    {
        InitializeComponent();
        _localData = ServiceHelper.GetRequiredService<LocalDataService>();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        _records = await _localData.LoadAsync();
        HistoryList.ItemsSource = _records.OrderByDescending(r => r.StartUtc).ToList();

        var now = DateTimeOffset.UtcNow;
        TimeSpan totalAll = TimeSpan.FromSeconds(_records.Sum(r => r.Duration.TotalSeconds));
        TimeSpan totalDay = TimeSpan.FromSeconds(_records.Where(r => r.StartUtc.Date == now.Date).Sum(r => r.Duration.TotalSeconds));
        TimeSpan totalWeek = TimeSpan.FromSeconds(
            _records.Where(r => ISOWeek.GetWeekOfYear(r.StartUtc.Date) == ISOWeek.GetWeekOfYear(now.Date) && r.StartUtc.Year == now.Year)
                    .Sum(r => r.Duration.TotalSeconds));
        TimeSpan totalMonth = TimeSpan.FromSeconds(_records.Where(r => r.StartUtc.Year == now.Year && r.StartUtc.Month == now.Month).Sum(r => r.Duration.TotalSeconds));

        TotalsLabel.Text = $"Heute: {totalDay:c}  |  Woche: {totalWeek:c}  |  Monat: {totalMonth:c}  |  Gesamt: {totalAll:c}";
    }

    private async void OnReload(object? sender, EventArgs e)
    {
        await LoadAsync();
    }
}
