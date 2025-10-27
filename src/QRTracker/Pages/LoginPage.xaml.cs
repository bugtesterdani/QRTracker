using QRTracker.Services;
using QRTracker.Helpers;

namespace QRTracker.Pages;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _auth;
    private readonly SettingsService _settings;
    private readonly SharePointService _sp;

    public LoginPage()
    {
        InitializeComponent();
        _auth = ServiceHelper.GetRequiredService<AuthService>();
        _settings = ServiceHelper.GetRequiredService<SettingsService>();
        _sp = ServiceHelper.GetRequiredService<SharePointService>();
        _ = InitAsync();
    }

    private async Task InitAsync()
    {
        var s = await _settings.LoadAsync();
        await _auth.InitializeAsync(s);
    }

    private async void OnLogin(object? sender, EventArgs e)
    {
        var res = await _auth.InteractiveAsync();
        if (res.Success)
        {
            var s = await _settings.LoadAsync();
            _sp.Configure(s, res.AccessToken);
            await DisplayAlert("OK", $"Angemeldet: {res.AccountUpn}", "OK");
        }
        else
        {
            await DisplayAlert("Fehler", res.Error ?? "Unbekannt", "OK");
        }
    }
}
