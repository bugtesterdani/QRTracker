using System;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace QRTracker.UITests;

[TestFixture]
public class MainPageWindowsTests
{
    private WindowsDriver<WindowsElement>? _driver;
    private WindowsDriver<WindowsElement> Driver => _driver ?? throw new InvalidOperationException("WinAppDriver session not initialised.");

    [SetUp]
    public void SetUp()
    {
        var appPath = TestCredentialsOptional("WindowsAppPath");
        if (string.IsNullOrWhiteSpace(appPath))
        {
            Assert.Ignore("Windows UI test skipped: 'WindowsAppPath' not set in credentials.txt.");
        }

        if (!File.Exists(appPath))
        {
            Assert.Ignore($"Windows UI test skipped: '{appPath}' does not exist.");
        }

        var uriText = TestCredentialsOptional("WinAppDriverUrl") ?? "http://127.0.0.1:4723";
        var serverUri = new Uri(uriText);

        var options = new AppiumOptions();
        options.AddAdditionalCapability("app", appPath);
        options.AddAdditionalCapability("deviceName", "WindowsPC");

        try
        {
            _driver = new WindowsDriver<WindowsElement>(serverUri, options, TimeSpan.FromSeconds(30));
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
        }
        catch (WebDriverException ex)
        {
            Assert.Ignore($"Windows UI test skipped: {ex.Message}. Ensure WinAppDriver is running at {serverUri}.");
        }
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            _driver?.Quit();
            _driver?.Dispose();
        }
        catch
        {
            // ignore cleanup errors
        }
        finally
        {
            _driver = null;
        }
    }

    [Test]
    public void AuthStatusLabel_showsLoginRequired()
    {
        var status = Driver.FindElementByAccessibilityId("AuthStatusLabel");
        Assert.That(status.Text, Does.Contain("Anmeldung"), "Expected AuthStatusLabel to indicate login requirement.");
    }

    [Test]
    public void StationAndDeviceEntries_areDisabled()
    {
        var station = Driver.FindElementByAccessibilityId("StationEntry");
        var device = Driver.FindElementByAccessibilityId("DeviceEntry");
        var startButton = Driver.FindElementByAccessibilityId("StartButton");

        Assert.That(station.Enabled, Is.False, "StationEntry should be disabled before login.");
        Assert.That(device.Enabled, Is.False, "DeviceEntry should be disabled before login.");
        Assert.That(startButton.Enabled, Is.False, "StartButton should be disabled before login.");
    }

    [Test]
    public void LoginScanButton_canBeClicked()
    {
        var loginButton = WaitForElementByAccessibilityId("LoginButton");
        loginButton.Click();
        HandlePostClick("LoginButton");

        var scanButton = WaitForElementByAccessibilityId("LoginScanButton");

        scanButton.Click();
        HandlePostClick("LoginScanButton");

        var configScanPage = WaitForOptionalElementByAccessibilityId("ConfigScanPage", TimeSpan.FromSeconds(5));
        if (configScanPage is not null)
        {
            var cancelButton = WaitForElementByAccessibilityId("ConfigScanCancelButton");
            cancelButton.Click();
            HandlePostClick("ConfigScanCancelButton");
            WaitForElementByAccessibilityId("LoginButton");
        }
        else
        {
            // On unsupported setups the scan button just shows a hint dialog and leaves us on LoginPage.
            var loginContextElement = FindElementByAccessibilityId("LoginPage")
                                      ?? FindElementByAccessibilityId("LoginActionButton")
                                      ?? FindElementByName("Jetzt anmelden");
            Assert.That(loginContextElement, Is.Not.Null, "Login dialog should remain visible after clicking the scan button on unsupported platforms.");
        }

        // The email entry must stay enabled for manual input.
        var emailEntry = WaitForElementByAccessibilityId("LoginEmailEntry");
        Assert.That(emailEntry.Enabled, Is.True, "Email entry should remain enabled after scan attempt.");
    }

    private static string? TestCredentialsOptional(string key)
    {
        try
        {
            return TestCredentials.Get(key);
        }
        catch
        {
            return null;
        }
    }

    private void HandlePostClick(string context)
    {
        Thread.Sleep(TimeSpan.FromSeconds(2));
        if (DismissAlertIfPresent(context))
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }
    }

    private WindowsElement WaitForElementByAccessibilityId(string automationId, TimeSpan? timeout = null)
    {
        var element = FindElementWithRetry(() => FindElementByAccessibilityId(automationId), timeout);
        if (element == null)
        {
            Assert.Fail($"Element with AutomationId '{automationId}' could not be located.");
        }
        return element!;
    }

    private WindowsElement? FindElementByAccessibilityId(string automationId) =>
        Driver.FindElementsByAccessibilityId(automationId).FirstOrDefault();

    private WindowsElement? FindElementByName(string name) =>
        Driver.FindElementsByName(name).FirstOrDefault();

    private WindowsElement? FindElementWithRetry(Func<WindowsElement?> locator, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        while (DateTime.UtcNow < deadline)
        {
            var element = locator();
            if (element is not null)
            {
                return element;
            }
            Thread.Sleep(200);
        }

        return locator();
    }

    private WindowsElement? WaitForOptionalElementByAccessibilityId(string automationId, TimeSpan? timeout = null) =>
        FindElementWithRetry(() => FindElementByAccessibilityId(automationId), timeout);

    private bool DismissAlertIfPresent(string context)
    {
        try
        {
            var okButtons = Driver.FindElementsByName("OK").ToList();
            if (okButtons.Count > 0)
            {
                okButtons[0].Click();
                TestContext.Progress.WriteLine($"[WARN] DisplayAlert acknowledged after '{context}'.");
                return true;
            }
        }
        catch (WebDriverException ex)
        {
            TestContext.Progress.WriteLine($"[WARN] Failed to handle potential alert after '{context}': {ex.Message}");
        }

        return false;
    }
}
