using QRTracker.Models;
using QRTracker.Services;
using QRTracker.Helpers;
#if ANDROID || IOS || MACCATALYST
using ZXing.Net.Maui.Controls;
#endif

namespace QRTracker;

public partial class MainPage : ContentPage
{
    private readonly SettingsService _settingsService;
    private readonly AuthService _authService;
    private readonly LocalDataService _localData;
    private readonly SharePointService _spService;

    private AppSettings _settings = new();
    private DateTimeOffset? _startUtc;
    private string? _station;
    private string? _device;
    private IDispatcherTimer? _timer;
    private string? _accessToken;

    public MainPage()
    {
        InitializeComponent();
        _settingsService = ServiceHelper.GetRequiredService<SettingsService>();
        _authService = ServiceHelper.GetRequiredService<AuthService>();
        _localData = ServiceHelper.GetRequiredService<LocalDataService>();
        _spService = ServiceHelper.GetRequiredService<SharePointService>();
        _ = InitAsync();
    }

    private async Task InitAsync()
    {
        _settings = await _settingsService.LoadAsync();
        await _authService.InitializeAsync(_settings);

        if (_settings.TrySilentSsoOnStart)
        {
            var silent = await _authService.TrySilentAsync();
            if (silent.Success)
            {
                _accessToken = silent.AccessToken;
                _spService.Configure(_settings, _accessToken);
                AuthStatus.Text = $"Angemeldet: {silent.AccountUpn}";
            }
            else
            {
                AuthStatus.Text = "Nicht angemeldet";
            }
        }
        else
        {
            AuthStatus.Text = "Login optional";
        }

#if ANDROID || IOS || MACCATALYST
        try
        {
            var cam = new CameraBarcodeReaderView
            {
                IsDetecting = true,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };
            cam.BarcodesDetected += OnBarcodeDetected;
            MobileScanHost.Children.Clear();
            MobileScanHost.Children.Add(cam);
        }
        catch
        {
            // ignore scanner issues on platforms without camera
        }
#endif
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        var res = await _authService.InteractiveAsync();
        if (res.Success)
        {
            _accessToken = res.AccessToken;
            _spService.Configure(_settings, _accessToken);
            AuthStatus.Text = $"Angemeldet: {res.AccountUpn}";
        }
        else
        {
            await DisplayAlert("Login fehlgeschlagen", res.Error ?? "Unbekannter Fehler", "OK");
        }
    }

    private void OnStart(object? sender, EventArgs e)
    {
        var s = StationEntry.Text?.Trim();
        var g = DeviceEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(s) || !s.StartsWith('S'))
        {
            DisplayAlert("Fehler", "Station-Code muss mit S beginnen.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(g) || !g.StartsWith('G'))
        {
            DisplayAlert("Fehler", "Geräte-Code muss mit G beginnen.", "OK");
            return;
        }
        _station = s;
        _device = g;
        _startUtc = DateTimeOffset.UtcNow;

        _timer?.Stop();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) => UpdateTimer();
        _timer.Start();
        UpdateTimer();
    }

    private async void OnStopManual(object? sender, EventArgs e)
    {
        await CompleteAsync();
    }

    private void UpdateTimer()
    {
        if (_startUtc is null)
        {
            TimerLabel.Text = "-";
            return;
        }
        var elapsed = DateTimeOffset.UtcNow - _startUtc.Value;
        TimerLabel.Text = $"Laufzeit: {elapsed:hh\\:mm\\:ss}";
    }

    private async void OnBarcodeDetected(object? sender, ZXing.Net.Maui.BarcodeDetectionEventArgs e)
    {
        var value = e.Results?.FirstOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(value)) return;
        await Dispatcher.DispatchAsync(async () =>
        {
            if (_startUtc is null)
            {
                if (_station is null && value.StartsWith('S'))
                {
                    _station = value;
                    StationEntry.Text = _station;
                }
                else if (_station is not null && _device is null && value.StartsWith('G'))
                {
                    _device = value;
                    DeviceEntry.Text = _device;
                    OnStart(this, EventArgs.Empty);
                }
            }
            else
            {
                if (value.StartsWith('G') && string.Equals(value, _device, StringComparison.Ordinal))
                {
                    await CompleteAsync();
                }
            }
        });
    }

    private async Task CompleteAsync()
    {
        if (_startUtc is null || _station is null || _device is null) return;
        _timer?.Stop();

        var action = await DisplayActionSheet("Was wurde gemacht?", "Abbrechen", null, "W", "R", "P", "S");
        if (string.IsNullOrWhiteSpace(action)) return;
        var note = await DisplayPromptAsync("Notiz", "Optionale Notiz eingeben:");

        var rec = new SessionRecord
        {
            StationCode = _station!,
            DeviceCode = _device!,
            StartUtc = _startUtc.Value,
            EndUtc = DateTimeOffset.UtcNow,
            ActionCode = action!,
            Note = note
        };

        await _localData.AppendAsync(rec);

        bool uploaded = false;
        if (_settings.UseSharePoint)
        {
            try { uploaded = await _spService.AppendExcelRowAsync(rec); }
            catch { }
        }

        await DisplayAlert("Erfasst", uploaded ? "Lokal gespeichert und hochgeladen." : "Lokal gespeichert (Upload später möglich).", "OK");

        _startUtc = null;
        _station = null;
        _device = null;
        StationEntry.Text = string.Empty;
        DeviceEntry.Text = string.Empty;
        UpdateTimer();
    }
}
