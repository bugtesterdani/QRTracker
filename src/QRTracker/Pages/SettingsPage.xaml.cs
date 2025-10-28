using System.Text.Json;
using QRTracker.Models;
using QRTracker.Services;
using QRTracker.Helpers;

namespace QRTracker.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsService _settingsService;
    private AppSettings _settings = new();
    private bool _detailsVisible;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

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
        SetDetailsVisibility(false);
        UpdateQrCode();
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
        UpdateQrCode();
        await DisplayAlert("Gespeichert", "Einstellungen gespeichert.", "OK");
    }

    private void UpdateQrCode()
    {
        var payload = ConfigurationPayload.FromSettings(_settings);
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        if (string.IsNullOrWhiteSpace(payload.ClientId) || string.IsNullOrWhiteSpace(payload.TenantId))
        {
            ConfigQr.Value = string.Empty;
            QrStatusLabel.Text = "Konfiguration unvollständig. Bitte Details ausfüllen.";
        }
        else
        {
            ConfigQr.Value = json;
            QrStatusLabel.Text = string.IsNullOrWhiteSpace(_settings.UserEmail)
                ? "Konfiguration bereit."
                : $"Konfiguration für {_settings.UserEmail}";
        }
    }

    private void OnToggleDetails(object? sender, EventArgs e)
    {
        SetDetailsVisibility(!_detailsVisible);
    }

    private void SetDetailsVisibility(bool visible)
    {
        _detailsVisible = visible;
        DetailsContainer.IsVisible = visible;
        ToggleDetailsButton.Text = visible ? "Details ausblenden" : "Details anzeigen";
    }
}
