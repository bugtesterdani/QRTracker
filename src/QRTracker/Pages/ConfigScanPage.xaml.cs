using System.Text.Json;
using ZXing.Net.Maui;
using QRTracker.Models;

namespace QRTracker.Pages;

public partial class ConfigScanPage : ContentPage
{
    private readonly Func<ConfigurationPayload, Task> _onConfigurationScanned;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private bool _handled;

    public ConfigScanPage(Func<ConfigurationPayload, Task> onConfigurationScanned)
    {
        InitializeComponent();
        _onConfigurationScanned = onConfigurationScanned;
        EnsureOptions();
        CameraView.HandlerChanged += (_, _) => EnsureOptions();
    }

    private void EnsureOptions()
    {
        CameraView.Options = new BarcodeReaderOptions
        {
            AutoRotate = true,
            Multiple = false,
            Formats = BarcodeFormat.QrCode
        };
    }

    private async void OnBarcodesDetected(object? sender, ZXing.Net.Maui.BarcodeDetectionEventArgs e)
    {
        if (_handled)
            return;

        var value = e.Results?.FirstOrDefault()?.Value;
        if (string.IsNullOrWhiteSpace(value))
            return;

        _handled = true;

        await Dispatcher.DispatchAsync(async () =>
        {
            try
            {
                var payload = JsonSerializer.Deserialize<ConfigurationPayload>(value, _jsonOptions);
                if (payload == null || string.IsNullOrWhiteSpace(payload.ClientId) || string.IsNullOrWhiteSpace(payload.TenantId))
                {
                    StatusLabel.Text = "Ungültiger QR-Code.";
                    _handled = false;
                    return;
                }

                await _onConfigurationScanned(payload);
                await Navigation.PopModalAsync();
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Fehler beim Lesen: {ex.Message}";
                _handled = false;
            }
        });
    }

    private async void OnCancel(object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
