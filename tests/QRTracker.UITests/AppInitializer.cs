using System;
using System.IO;
using Xamarin.UITest;

namespace QRTracker.UITests;

public static class AppInitializer
{
    public static IApp StartApp(Platform platform)
    {
        return platform switch
        {
            Platform.Android => ConfigureApp.Android.ApkFile(ResolvePath("QRTRACKER_ANDROID_APP")).StartApp(),
            Platform.iOS => ConfigureApp.iOS.AppBundle(ResolvePath("QRTRACKER_IOS_APP")).StartApp(),
            _ => throw new NotSupportedException($"Platform '{platform}' is not supported for these UI tests.")
        };
    }

    private static string ResolvePath(string environmentVariable)
    {
        var path = Environment.GetEnvironmentVariable(environmentVariable);
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException($"Set the environment variable '{environmentVariable}' to the built app path before running the UI tests.");
        }

        if (File.Exists(path) || Directory.Exists(path))
        {
            return Path.GetFullPath(path);
        }

        throw new FileNotFoundException($"The path '{path}' defined in '{environmentVariable}' was not found. Verify the build output and try again.", path);
    }
}
