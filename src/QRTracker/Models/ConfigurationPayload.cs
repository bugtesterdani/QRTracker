using System.Text.Json.Serialization;

namespace QRTracker.Models;

public class ConfigurationPayload
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("preferredUserHint")]
    public string? PreferredUserHint { get; set; }

    [JsonPropertyName("siteId")]
    public string? SiteId { get; set; }

    [JsonPropertyName("driveId")]
    public string? DriveId { get; set; }

    [JsonPropertyName("itemId")]
    public string? ItemId { get; set; }

    [JsonPropertyName("tableName")]
    public string? TableName { get; set; }

    [JsonPropertyName("useSharePoint")]
    public bool UseSharePoint { get; set; }

    [JsonPropertyName("scopes")]
    public string[] Scopes { get; set; } = Array.Empty<string>();

    public static ConfigurationPayload FromSettings(AppSettings settings)
    {
        return new ConfigurationPayload
        {
            Email = string.IsNullOrWhiteSpace(settings.UserEmail) ? null : settings.UserEmail,
            TenantId = settings.TenantId ?? string.Empty,
            ClientId = settings.ClientId ?? string.Empty,
            PreferredUserHint = settings.PreferredUserHint,
            SiteId = settings.SiteId,
            DriveId = settings.DriveId,
            ItemId = settings.ItemId,
            TableName = settings.TableName,
            UseSharePoint = settings.UseSharePoint,
            Scopes = settings.Scopes
        };
    }

    public void Apply(AppSettings settings)
    {
        settings.UserEmail = Email ?? settings.UserEmail;
        settings.TenantId = TenantId;
        settings.ClientId = ClientId;
        settings.PreferredUserHint = PreferredUserHint;
        settings.SiteId = SiteId;
        settings.DriveId = DriveId;
        settings.ItemId = ItemId;
        if (!string.IsNullOrWhiteSpace(TableName))
        {
            settings.TableName = TableName!;
        }
        settings.UseSharePoint = UseSharePoint;
        if (Scopes is { Length: > 0 })
        {
            settings.Scopes = Scopes;
        }
    }
}

