using QRTracker.Models;
using QRTracker.Services;
using QRTracker.Helpers;

namespace QRTracker.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsService _settingsService;
    private AppSettings _settings = new();

    public SettingsPage()
    {
        InitializeComponent();
        _settingsService = ServiceHelper.GetRequiredService<SettingsService>();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        _settings = await _settingsService.LoadAsync();
        EmailEntry.Text = _settings.UserEmail;
        TenantEntry.Text = _settings.TenantId;
        ClientEntry.Text = _settings.ClientId;
        UserHintEntry.Text = _settings.PreferredUserHint;
        SilentSwitch.IsToggled = _settings.TrySilentSsoOnStart;
        UseSharePointSwitch.IsToggled = _settings.UseSharePoint;
        SiteIdEntry.Text = _settings.SiteId;
        DriveIdEntry.Text = _settings.DriveId;
        ItemIdEntry.Text = _settings.ItemId;
        TableNameEntry.Text = _settings.TableName;
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        _settings.UserEmail = EmailEntry.Text?.Trim() ?? string.Empty;
        _settings.TenantId = TenantEntry.Text?.Trim();
        _settings.ClientId = ClientEntry.Text?.Trim();
        _settings.PreferredUserHint = UserHintEntry.Text?.Trim();
        _settings.TrySilentSsoOnStart = SilentSwitch.IsToggled;
        _settings.UseSharePoint = UseSharePointSwitch.IsToggled;
        _settings.SiteId = SiteIdEntry.Text?.Trim();
        _settings.DriveId = DriveIdEntry.Text?.Trim();
        _settings.ItemId = ItemIdEntry.Text?.Trim();
        _settings.TableName = string.IsNullOrWhiteSpace(TableNameEntry.Text) ? _settings.TableName : TableNameEntry.Text!.Trim();
        await _settingsService.SaveAsync(_settings);
        await DisplayAlert("Gespeichert", "Einstellungen gespeichert.", "OK");
    }
}
