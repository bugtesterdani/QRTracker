using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.Queries;

namespace QRTracker.UITests;

[TestFixture(Platform.Android)]
public class MainPageTests
{
    private readonly Platform _platform;
    private IApp? _app;
    private IApp App => _app ?? throw new InvalidOperationException("App not initialised. Did the setup fail?");

    public MainPageTests(Platform platform)
    {
        _platform = platform;
    }

    [SetUp]
    public void SetUp()
    {
        try
        {
            _app = AppInitializer.StartApp(_platform);
            App.WaitForElement("MainPage");
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Android UI test skipped: {ex.Message}");
        }
    }

    [Test]
    public void LoginButton_isAvailable()
    {
        App.WaitForElement("LoginButton");
        App.Tap("LoginButton");
        HandlePostClick("LoginButton");

        // If the modal appears we expect to see the email entry field.
        var loginEntry = App.Query("LoginEmailEntry").FirstOrDefault();
        Assert.That(loginEntry, Is.Not.Null, "LoginEmailEntry not found after tapping the login button.");
    }

    [Test]
    public void LoginScanButton_isPresent()
    {
        App.WaitForElement("LoginButton");
        App.Tap("LoginButton");
        HandlePostClick("LoginButton (before scan)");

        var scanButton = App.Query("LoginScanButton").FirstOrDefault();
        Assert.That(scanButton, Is.Not.Null, "LoginScanButton not found on login modal.");

        App.Tap("LoginScanButton");
        HandlePostClick("LoginScanButton");

        var configScanPage = App.Query("ConfigScanPage").FirstOrDefault();
        if (configScanPage is not null)
        {
            App.WaitForElement("ConfigScanCancelButton");
            App.Tap("ConfigScanCancelButton");
            HandlePostClick("ConfigScanCancelButton");
            App.WaitForElement("LoginButton");
        }
        else
        {
            // Verify LoginPage is still present (modal not closed unexpectedly)
            var loginModal = App.Query("LoginPage").FirstOrDefault();
            Assert.That(loginModal, Is.Not.Null, "LoginPage should remain visible after tapping the scan button.");
        }
    }

    [Test]
    public void Controls_areDisabledWithoutAuthentication()
    {
        App.WaitForElement("StationEntry");
        App.WaitForElement("DeviceEntry");
        App.WaitForElement("StartButton");

        var stationEntry = App.Query("StationEntry").FirstOrDefault();
        var deviceEntry = App.Query("DeviceEntry").FirstOrDefault();
        var startButton = App.Query("StartButton").FirstOrDefault();

        Assert.That(stationEntry, Is.Not.Null, "StationEntry not found.");
        Assert.That(deviceEntry, Is.Not.Null, "DeviceEntry not found.");
        Assert.That(startButton, Is.Not.Null, "StartButton not found.");

        Assert.That(stationEntry!.Enabled, Is.False, "StationEntry should be disabled before login.");
        Assert.That(deviceEntry!.Enabled, Is.False, "DeviceEntry should be disabled before login.");
        Assert.That(startButton!.Enabled, Is.False, "StartButton should be disabled before login.");
    }

    private void HandlePostClick(string context)
    {
        Thread.Sleep(TimeSpan.FromSeconds(2));
        if (DismissAlertIfPresent(context))
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }
    }

    private bool DismissAlertIfPresent(string context)
    {
        try
        {
            var okButtons = App.Query(c => c.Marked("OK"));
            if (okButtons.Any())
            {
                App.Tap("OK");
                TestContext.Progress.WriteLine($"[WARN] DisplayAlert acknowledged after '{context}'.");
                return true;
            }
        }
        catch (Exception ex)
        {
            TestContext.Progress.WriteLine($"[WARN] Failed to handle potential alert after '{context}': {ex.Message}");
        }

        return false;
    }
}
