namespace QRTracker.Models;

public class AppSettings
{
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string[] Scopes { get; set; } = new[] { "Files.ReadWrite.All", "Sites.ReadWrite.All", "offline_access" };

    // SharePoint/Excel targets
    public string? SiteId { get; set; }
    public string? DriveId { get; set; }
    public string? ItemId { get; set; } // Excel file item id
    public string TableName { get; set; } = "Table1";

    // Behavior
    public bool UseSharePoint { get; set; } = false;
    public bool TrySilentSsoOnStart { get; set; } = true;

    // Misc
    public string? PreferredUserHint { get; set; }
}

