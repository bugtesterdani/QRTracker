using Microsoft.Extensions.Logging;
using QRTracker.Services;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace QRTracker;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureMauiHandlers(handlers =>
            {
                handlers.AddHandler<CameraBarcodeReaderView, CameraBarcodeReaderViewHandler>();
                handlers.AddHandler<BarcodeGeneratorView, BarcodeGeneratorViewHandler>();
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // DI services
        builder.Services.AddSingleton<SettingsService>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<LocalDataService>();
        builder.Services.AddSingleton<SharePointService>();

        return builder.Build();
    }
}
