using QRTracker.Models;

namespace QRTracker.Services;

public record TenantConfiguration(
    string TenantId,
    string ClientId,
    string? PreferredUserHint,
    string? SiteId,
    string? DriveId,
    string? ItemId,
    string? TableName,
    bool UseSharePoint);

public static class TenantConfigProvider
{
    private static readonly Dictionary<string, TenantConfiguration> _configByDomain =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // TODO: Replace the sample entry with your corporate domains and configuration values
            // ["example.com"] = new TenantConfiguration(
            //     TenantId: "00000000-0000-0000-0000-000000000000",
            //     ClientId: "00000000-0000-0000-0000-000000000000",
            //     PreferredUserHint: null,
            //     SiteId: "{site-id}",
            //     DriveId: "{drive-id}",
            //     ItemId: "{item-id}",
            //     TableName: "Table1",
            //     UseSharePoint: true)
        };

    public static bool TryGetConfiguration(string email, out TenantConfiguration configuration)
    {
        configuration = default!;
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var domainIndex = email.IndexOf('@');
        if (domainIndex < 0 || domainIndex >= email.Length - 1)
        {
            return false;
        }

        var domain = email[(domainIndex + 1)..];
        if (_configByDomain.TryGetValue(domain, out var config))
        {
            configuration = config;
            return true;
        }

        configuration = default!;
        return false;
    }

    public static void ApplyToSettings(AppSettings settings, TenantConfiguration configuration, string email)
    {
        settings.UserEmail = email;
        settings.TenantId = configuration.TenantId;
        settings.ClientId = configuration.ClientId;
        settings.PreferredUserHint = configuration.PreferredUserHint;
        settings.SiteId = configuration.SiteId;
        settings.DriveId = configuration.DriveId;
        settings.ItemId = configuration.ItemId;
        if (!string.IsNullOrWhiteSpace(configuration.TableName))
        {
            settings.TableName = configuration.TableName;
        }
        settings.UseSharePoint = configuration.UseSharePoint;
    }

    public static void RegisterOrUpdate(string domain, TenantConfiguration configuration)
    {
        _configByDomain[domain] = configuration;
    }
}
