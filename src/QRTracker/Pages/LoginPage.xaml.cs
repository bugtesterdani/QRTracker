using System.Net.Mail;
using QRTracker.Services;
using QRTracker.Helpers;

namespace QRTracker.Pages;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _auth;
    private readonly SettingsService _settings;
    private readonly SharePointService _sp;
    private bool _isProcessing;
    private readonly Func<Task>? _requestSettingsCallback;

    public LoginPage(Func<Task>? requestSettingsCallback = null)
    {
        InitializeComponent();
        _requestSettingsCallback = requestSettingsCallback;
        _auth = ServiceHelper.GetRequiredService<AuthService>();
        _settings = ServiceHelper.GetRequiredService<SettingsService>();
        _sp = ServiceHelper.GetRequiredService<SharePointService>();
        _ = InitAsync();
    }

    private async Task InitAsync()
    {
        var s = await _settings.LoadAsync();
        await _auth.InitializeAsync(s);
        if (!string.IsNullOrWhiteSpace(s.UserEmail))
        {
            EmailEntry.Text = s.UserEmail;
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
            var settings = await _settings.LoadAsync();
            var rawEmail = EmailEntry.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(rawEmail))
            {
                await DisplayAlert("Hinweis", "Bitte eine gültige Firmen-E-Mail-Adresse angeben.", "OK");
                return;
            }

            string email;
            try
            {
                email = new MailAddress(rawEmail).Address;
            }
            catch
            {
                await DisplayAlert("Hinweis", "Bitte eine gültige Firmen-E-Mail-Adresse angeben.", "OK");
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
                    await DisplayAlert("Konfiguration erforderlich", "Für diese Domäne ist noch keine Zuordnung hinterlegt. Bitte wende dich an die IT oder trage die Daten in den Einstellungen ein.", "OK");
                    if (_requestSettingsCallback is not null)
                    {
                        await _requestSettingsCallback();
                    }
                    return;
                }
            }

            await _settings.SaveAsync(settings);
            await _auth.InitializeAsync(settings);

            var res = await _auth.InteractiveAsync();
            if (res.Success)
            {
                _sp.Configure(settings, res.AccessToken);
                await DisplayAlert("Erfolg", $"Angemeldet: {res.AccountUpn}", "OK");
            }
            else
            {
                await DisplayAlert("Fehler", res.Error ?? "Unbekannt", "OK");
            }
        }
        finally
        {
            LoginActionButton.IsEnabled = true;
            _isProcessing = false;
        }
    }

    protected override bool OnBackButtonPressed() => true;
}
