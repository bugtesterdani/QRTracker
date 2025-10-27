namespace QRTracker.Helpers;

public static class ServiceHelper
{
    public static T GetRequiredService<T>() where T : notnull
    {
        var sp = Application.Current?.Handler?.MauiContext?.Services
                 ?? throw new InvalidOperationException("ServiceProvider not ready");
        return sp.GetService<T>() ?? throw new InvalidOperationException($"Service {typeof(T)} not registered");
    }
}

