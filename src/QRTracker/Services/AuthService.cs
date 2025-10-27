using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using QRTracker.Models;

namespace QRTracker.Services;

public class AuthService
{
    private IPublicClientApplication? _pca;
    private AppSettings? _settings;

    public event EventHandler<AuthStateChangedEventArgs>? AuthenticationStateChanged;

    public bool IsAuthenticated { get; private set; }
    public string? CurrentAccountUpn { get; private set; }

    public async Task InitializeAsync(AppSettings settings)
    {
        _settings = settings;
        if (string.IsNullOrWhiteSpace(settings.ClientId) || string.IsNullOrWhiteSpace(settings.TenantId))
        {
            _pca = null;
            NotifySignedOut();
            return;
        }

        var builder = PublicClientApplicationBuilder.Create(settings.ClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, settings.TenantId);

#if ANDROID || IOS
        // Default redirect URI recommendation for MSAL on mobile
        builder = builder.WithRedirectUri($"msal{settings.ClientId}://auth");
#endif

        _pca = builder.Build();

        // Cross-platform secure cache (best-effort)
        try
        {
            var storageProps = new StorageCreationPropertiesBuilder("QRTracker.msalcache", FileSystem.AppDataDirectory)
                .WithMacKeyChain("QRTracker", "QRTracker.MSAL")
                .Build();
            var cacheHelper = await MsalCacheHelper.CreateAsync(storageProps);
            cacheHelper.RegisterCache(_pca.UserTokenCache);
        }
        catch
        {
            // Ignore cache binding failures; MSAL will still use in-memory cache
        }
    }

    public async Task<(bool Success, string? AccessToken, string? AccountUpn, string? Error)> TrySilentAsync()
    {
        if (_pca == null || _settings == null)
        {
            NotifySignedOut();
            return (false, null, null, "Not initialized");
        }

        try
        {
            var accounts = await _pca.GetAccountsAsync();
            var account = accounts.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(_settings.PreferredUserHint))
            {
                account = accounts.FirstOrDefault(a => a.Username?.Equals(_settings.PreferredUserHint, StringComparison.OrdinalIgnoreCase) == true) ?? account;
            }
            if (account == null)
            {
                NotifySignedOut();
                return (false, null, null, "No account");
            }

            var result = await _pca.AcquireTokenSilent(_settings.Scopes, account).ExecuteAsync();
            NotifyAuthenticated(result.AccessToken, result.Account.Username);
            return (true, result.AccessToken, result.Account.Username, null);
        }
        catch (MsalUiRequiredException)
        {
            NotifySignedOut();
            return (false, null, null, null);
        }
        catch (Exception ex)
        {
            NotifySignedOut();
            return (false, null, null, ex.Message);
        }
    }

    public async Task<(bool Success, string? AccessToken, string? AccountUpn, string? Error)> InteractiveAsync()
    {
        if (_pca == null || _settings == null)
        {
            NotifySignedOut();
            return (false, null, null, "Not initialized");
        }

        try
        {
            var builder = _pca.AcquireTokenInteractive(_settings.Scopes);
#if WINDOWS
            builder = builder.WithUseEmbeddedWebView(false);
#endif
            var result = await builder.ExecuteAsync();
            NotifyAuthenticated(result.AccessToken, result.Account.Username);
            return (true, result.AccessToken, result.Account.Username, null);
        }
        catch (Exception ex)
        {
            NotifySignedOut();
            return (false, null, null, ex.Message);
        }
    }

    private void NotifyAuthenticated(string? accessToken, string? accountUpn)
    {
        IsAuthenticated = true;
        CurrentAccountUpn = accountUpn;
        AuthenticationStateChanged?.Invoke(this, new AuthStateChangedEventArgs(true, accessToken, accountUpn));
    }

    private void NotifySignedOut()
    {
        IsAuthenticated = false;
        CurrentAccountUpn = null;
        AuthenticationStateChanged?.Invoke(this, new AuthStateChangedEventArgs(false, null, null));
    }
}

