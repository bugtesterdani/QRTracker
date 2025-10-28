using System.Net.Mail;
using QRTracker.Helpers;
using QRTracker.Models;
using QRTracker.Services;

namespace QRTracker.Pages;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _auth;
    private readonly SettingsService _settingsService;
    private readonly SharePointService _sharePoint;
    private readonly Func<Task>? _requestSettingsCallback;

    private bool _isProcessing;

    public LoginPage(Func<Task>? requestSettingsCallback = null)
    {
        InitializeComponent();
        _requestSettingsCallback = requestSettingsCallback;
        _auth = ServiceHelper.GetRequiredService<AuthService>();
        _settingsService = ServiceHelper.GetRequiredService<SettingsService>();
        _sharePoint = ServiceHelper.GetRequiredService<SharePointService>();
        _ = InitAsync();
    }

    private async Task InitAsync()
    {
        var settings = await _settingsService.LoadAsync();
        await _auth.InitializeAsync(settings);
        if (!string.IsNullOrWhiteSpace(settings.UserEmail))
        {
            EmailEntry.Text = settings.UserEmail;
        }
    }

    private async void OnLogin(object? sender, EventArgs e)
    {
        if (_isProcessing)
            return;

        _isProcessing = true;
        LoginActionButton.IsEnabled = false;

        try
        {
            var settings = await _settingsService.LoadAsync();

            var rawEmail = EmailEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(rawEmail))
            {
                rawEmail = settings.UserEmail;
            }

            if (string.IsNullOrWhiteSpace(rawEmail))
            {
                await DisplayAlert("Hinweis", "Bitte eine gueltige Firmen-E-Mail-Adresse angeben.", "OK");
                return;
            }

            string email;
            try
            {
                email = new MailAddress(rawEmail).Address;
            }
            catch
            {
                await DisplayAlert("Hinweis", "Bitte eine gueltige Firmen-E-Mail-Adresse angeben.", "OK");
                return;
            }

            settings.UserEmail = email;

            if (string.IsNullOrWhiteSpace(settings.ClientId) || string.IsNullOrWhiteSpace(settings.TenantId))
            {
                if (TenantConfigProvider.TryGetConfiguration(email, out var config))
                {
                    TenantConfigProvider.ApplyToSettings(settings, config, email);
                }
                else
                {
                    await DisplayAlert("Konfiguration erforderlich", "Fuer diese Domain ist noch keine Zuordnung hinterlegt. Bitte IT informieren oder Werte in den Einstellungen eintragen.", "OK");
                    if (_requestSettingsCallback is not null)
                    {
                        await _requestSettingsCallback();
                    }
                    return;
                }
            }

            await _settingsService.SaveAsync(settings);
            await _auth.InitializeAsync(settings);

            var result = await _auth.InteractiveAsync();
            if (result.Success)
            {
                _sharePoint.Configure(settings, result.AccessToken);
                await DisplayAlert("Erfolg", $"Angemeldet: {result.AccountUpn}", "OK");
            }
            else
            {
                await DisplayAlert("Fehler", result.Error ?? "Unbekannter Fehler", "OK");
            }
        }
        finally
        {
            LoginActionButton.IsEnabled = true;
            _isProcessing = false;
        }
    }

    private async void OnScan(object? sender, EventArgs e)
    {
        if (_isProcessing)
            return;

        var scanPage = new ConfigScanPage(ApplyConfigurationAsync);
        await Navigation.PushModalAsync(scanPage);
    }

    private async Task ApplyConfigurationAsync(ConfigurationPayload payload)
    {
        var settings = await _settingsService.LoadAsync();
        payload.Apply(settings);
        await _settingsService.SaveAsync(settings);
        await _auth.InitializeAsync(settings);

        if (!string.IsNullOrWhiteSpace(settings.UserEmail))
        {
            EmailEntry.Text = settings.UserEmail;
        }

        await DisplayAlert("Konfiguration", "Einstellungen aus dem QR-Code wurden uebernommen. Bitte melden Sie sich an.", "OK");
    }

    protected override bool OnBackButtonPressed() => true;
}




