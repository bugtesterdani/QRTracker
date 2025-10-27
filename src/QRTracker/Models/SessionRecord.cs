namespace QRTracker.Models;

public class SessionRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string StationCode { get; set; } = string.Empty; // starts with S
    public string DeviceCode { get; set; } = string.Empty;  // starts with G
    public DateTimeOffset StartUtc { get; set; }
    public DateTimeOffset EndUtc { get; set; }
    public string ActionCode { get; set; } = string.Empty; // W/R/P/S
    public string? Note { get; set; }

    public TimeSpan Duration => EndUtc - StartUtc;
}

