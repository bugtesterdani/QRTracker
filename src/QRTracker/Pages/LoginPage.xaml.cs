using QRTracker.Services;
using QRTracker.Helpers;

namespace QRTracker.Pages;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _auth;
    private readonly SettingsService _settings;
    private readonly SharePointService _sp;
    private bool _isProcessing;

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
        if (_isProcessing)
            return;

        _isProcessing = true;
        LoginActionButton.IsEnabled = false;

        var res = await _auth.InteractiveAsync();
        if (res.Success)
        {
            var s = await _settings.LoadAsync();
            _sp.Configure(s, res.AccessToken);
            await DisplayAlert("Erfolg", $"Angemeldet: {res.AccountUpn}", "OK");
        }
        else
        {
            await DisplayAlert("Fehler", res.Error ?? "Unbekannt", "OK");
        }

        LoginActionButton.IsEnabled = true;
        _isProcessing = false;
    }

    protected override bool OnBackButtonPressed() => true;
}
