using QRTracker.Models;
using QRTracker.Pages;
using QRTracker.Services;
using QRTracker.Helpers;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

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
    private bool _isLoginModalOpen;
    private Page? _loginModalPage;

	public MainPage()
	{
		InitializeComponent();
		_settingsService = ServiceHelper.GetRequiredService<SettingsService>();
		_authService = ServiceHelper.GetRequiredService<AuthService>();
		_localData = ServiceHelper.GetRequiredService<LocalDataService>();
		_spService = ServiceHelper.GetRequiredService<SharePointService>();
		_authService.AuthenticationStateChanged += OnAuthenticationStateChanged;
		ApplyAuthState(false);
		_ = InitAsync();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

        if (!_authService.IsAuthenticated)
		{
			await ShowLoginModalAsync();
		}
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
            }
        }

		if (!_authService.IsAuthenticated)
		{
			AuthStatus.Text = "Anmeldung erforderlich";
		}

#if ANDROID || IOS || MACCATALYST
        InitializeCameraScanner();
#endif
    }

    private void InitializeCameraScanner()
    {
        try
        {
#if ANDROID || IOS || MACCATALYST
            var cam = new CameraBarcodeReaderView
            {
                IsDetecting = true,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };
            ConfigureCameraOptions(cam);
            cam.BarcodesDetected += OnBarcodeDetected;
            MobileScanHost.Children.Clear();
            MobileScanHost.Children.Add(cam);
#endif
        }
        catch
        {
            // ignore scanner issues on platforms without camera or permissions
        }
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        await ShowLoginModalAsync();
    }

    private async void OnStart(object? sender, EventArgs e)
    {
        if (!_authService.IsAuthenticated)
        {
            await DisplayAlert("Login erforderlich", "Bitte melden Sie sich zuerst an.", "OK");
            await ShowLoginModalAsync();
            return;
        }

        var s = StationEntry.Text?.Trim();
        var g = DeviceEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(s) || !s.StartsWith('S'))
        {
            await DisplayAlert("Fehler", "Station-Code muss mit S beginnen.", "OK");
            return;
        }
        if (string.IsNullOrWhiteSpace(g) || !g.StartsWith('G'))
        {
            await DisplayAlert("Fehler", "Geraete-Code muss mit G beginnen.", "OK");
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
        if (!_authService.IsAuthenticated)
        {
            await ShowLoginModalAsync();
            return;
        }
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

		await DisplayAlert("Erfasst", uploaded ? "Lokal gespeichert und hochgeladen." : "Lokal gespeichert (Upload spaeter moeglich).", "OK");

        _startUtc = null;
        _station = null;
        _device = null;
        StationEntry.Text = string.Empty;
        DeviceEntry.Text = string.Empty;
        UpdateTimer();
    }

    private async void OnAuthenticationStateChanged(object? sender, AuthStateChangedEventArgs e)
    {
        await RunOnUiThreadAsync(async () =>
        {
            if (e.IsAuthenticated)
            {
                _accessToken = e.AccessToken;
                AuthStatus.Text = string.IsNullOrWhiteSpace(e.AccountUpn) ? "Angemeldet" : $"Angemeldet: {e.AccountUpn}";
                _spService.Configure(_settings, _accessToken);
                ApplyAuthState(true);
                await CloseLoginModalAsync();
            }
            else
            {
                _accessToken = null;
                AuthStatus.Text = "Anmeldung erforderlich";
                ApplyAuthState(false);
                await ShowLoginModalAsync();
            }
        });
    }

    private void ApplyAuthState(bool isAuthenticated)
    {
        StationEntry.IsEnabled = isAuthenticated;
        DeviceEntry.IsEnabled = isAuthenticated;
        StartButton.IsEnabled = isAuthenticated;
        StopButton.IsEnabled = isAuthenticated;

        if (!isAuthenticated)
        {
            _timer?.Stop();
            _timer = null;
            _startUtc = null;
            _station = null;
            _device = null;
            TimerLabel.Text = "Laufzeit: -";
            StationEntry.Text = string.Empty;
            DeviceEntry.Text = string.Empty;
        }
    }

    private async Task RunOnUiThreadAsync(Func<Task> action)
    {
        if (Dispatcher.IsDispatchRequired)
        {
            await Dispatcher.DispatchAsync(action);
        }
        else
        {
            await action();
        }
    }

    private static void ConfigureCameraOptions(CameraBarcodeReaderView view)
    {
        view.Options = new BarcodeReaderOptions
        {
            AutoRotate = true,
            Multiple = false,
            Formats = BarcodeFormat.QrCode | BarcodeFormat.Code128 | BarcodeFormat.Code39 | BarcodeFormat.Codabar
        };
    }

    private async Task ShowLoginModalAsync()
    {
        if (_authService.IsAuthenticated)
            return;

		if (_isLoginModalOpen)
			return;

		await RunOnUiThreadAsync(async () =>
		{
			if (_isLoginModalOpen)
				return;

			_isLoginModalOpen = true;
			_loginModalPage = new LoginPage(NavigateToSettingsAsync);
			await Navigation.PushModalAsync(_loginModalPage);
		});
	}

	private async Task CloseLoginModalAsync()
	{
		if (!_isLoginModalOpen)
			return;

		await RunOnUiThreadAsync(async () =>
		{
			if (Navigation.ModalStack.Count > 0)
			{
				await Navigation.PopModalAsync();
			}
			_loginModalPage = null;
			_isLoginModalOpen = false;
		});
	}

	private async Task NavigateToSettingsAsync()
	{
		await CloseLoginModalAsync();
		if (Shell.Current is not null)
		{
			await Shell.Current.GoToAsync("//SettingsPage");
		}
	}
}










