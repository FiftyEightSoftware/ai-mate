namespace ai_mate_blazor.Models;

public class TaxAuditEntry
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Action { get; set; } = string.Empty;
    public string? ProfileId { get; set; }
    public bool VatChanged { get; set; }
    public bool HmrcChanged { get; set; }
    public string? OldVat { get; set; }
    public string? NewVat { get; set; }
    public string? OldHmrc { get; set; }
    public string? NewHmrc { get; set; }
}
