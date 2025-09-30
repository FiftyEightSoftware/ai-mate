using System.Text.Json.Serialization;

namespace ai_mate_blazor.Models;

public class VatLookupResponse
{
    [JsonPropertyName("target")]
    public VatTarget? Target { get; set; }
}

public class VatTarget
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("address")]
    public VatAddress? Address { get; set; }
    
    [JsonPropertyName("vatNumber")]
    public string? VatNumber { get; set; }
}

public class VatAddress
{
    [JsonPropertyName("line1")]
    public string? Line1 { get; set; }
    
    [JsonPropertyName("line2")]
    public string? Line2 { get; set; }
    
    [JsonPropertyName("postcode")]
    public string? Postcode { get; set; }
    
    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; set; }
}

public enum ValidationState
{
    None,
    Validating,
    Valid,
    Invalid
}
