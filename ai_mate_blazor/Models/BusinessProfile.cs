namespace ai_mate_blazor.Models;

public class BusinessProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Default Business";
    public string? VatRegistrationId { get; set; }
    public string? HmrcGatewayId { get; set; }
    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
