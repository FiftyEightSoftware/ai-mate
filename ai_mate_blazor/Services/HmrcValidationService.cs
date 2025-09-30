using System.Net.Http.Json;
using System.Text.RegularExpressions;
using ai_mate_blazor.Models;

namespace ai_mate_blazor.Services;

public class HmrcValidationService
{
    private readonly HttpClient _httpClient;

    public HmrcValidationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Validates VAT number format (UK and EU formats)
    /// </summary>
    public bool ValidateVatNumberFormat(string? vat)
    {
        if (string.IsNullOrWhiteSpace(vat)) return true; // Optional field

        // Remove spaces and convert to uppercase
        var cleaned = vat.Replace(" ", "").Replace("-", "").ToUpperInvariant();

        // UK VAT: GB followed by 9 digits or 12 digits (GB + 9 digits + 3 suffix)
        if (cleaned.StartsWith("GB"))
        {
            var digits = cleaned.Substring(2);
            
            // Standard format: 9 digits
            if (Regex.IsMatch(digits, @"^\d{9}$")) return true;
            
            // Branch traders: 12 digits
            if (Regex.IsMatch(digits, @"^\d{12}$")) return true;
            
            // Government departments: GD followed by 3 digits
            if (Regex.IsMatch(digits, @"^GD\d{3}$")) return true;
            
            // Health authorities: HA followed by 3 digits
            if (Regex.IsMatch(digits, @"^HA\d{3}$")) return true;
            
            return false;
        }

        // EU VAT formats (basic validation)
        // Format: 2 letter country code + 2-12 alphanumeric characters
        return Regex.IsMatch(cleaned, @"^[A-Z]{2}[\w\d]{2,12}$");
    }

    /// <summary>
    /// Validates HMRC Gateway ID format
    /// </summary>
    public bool ValidateHmrcGatewayIdFormat(string? hmrcId)
    {
        if (string.IsNullOrWhiteSpace(hmrcId)) return true; // Optional field
        
        // HMRC Gateway IDs are typically 12-15 characters, alphanumeric
        var cleaned = hmrcId.Replace(" ", "").Replace("-", "");
        return cleaned.Length >= 6 && cleaned.Length <= 20 && 
               Regex.IsMatch(cleaned, @"^[A-Za-z0-9]+$");
    }

    /// <summary>
    /// Validates VAT number against HMRC API (requires API credentials)
    /// Note: This is a placeholder - requires proper HMRC API setup
    /// </summary>
    public async Task<(bool IsValid, string? CompanyName, VatAddress? Address)> ValidateVatNumberOnlineAsync(string vatNumber)
    {
        if (!ValidateVatNumberFormat(vatNumber))
        {
            return (false, null, null);
        }

        try
        {
            // Clean VAT number
            var cleaned = vatNumber.Replace(" ", "").Replace("-", "").ToUpperInvariant();
            
            // For production: Use actual HMRC API endpoint with authentication
            // var response = await _httpClient.GetAsync(
            //     $"https://api.service.hmrc.gov.uk/organisations/vat/check-vat-number/lookup/{cleaned}"
            // );
            
            // For now, return simulated validation based on format
            await Task.Delay(500); // Simulate network delay
            
            // In development, consider format validation as "valid"
            if (ValidateVatNumberFormat(vatNumber))
            {
                return (true, "Sample Company Ltd", new VatAddress
                {
                    Line1 = "123 Business Street",
                    Postcode = "SW1A 1AA",
                    CountryCode = "GB"
                });
            }

            return (false, null, null);
        }
        catch (Exception)
        {
            // If API call fails, fall back to format validation only
            return (ValidateVatNumberFormat(vatNumber), null, null);
        }
    }

    /// <summary>
    /// Gets user-friendly error message for invalid VAT number
    /// </summary>
    public string GetVatValidationError(string? vat)
    {
        if (string.IsNullOrWhiteSpace(vat)) return string.Empty;

        var cleaned = vat.Replace(" ", "").Replace("-", "").ToUpperInvariant();

        if (cleaned.Length < 2)
            return "VAT number is too short";

        if (!Regex.IsMatch(cleaned.Substring(0, 2), @"^[A-Z]{2}$"))
            return "VAT number must start with a 2-letter country code (e.g., GB)";

        if (cleaned.StartsWith("GB"))
        {
            var digits = cleaned.Substring(2);
            if (!Regex.IsMatch(digits, @"^(\d{9}|\d{12}|GD\d{3}|HA\d{3})$"))
                return "UK VAT number format is invalid. Expected: GB followed by 9 or 12 digits";
        }
        else
        {
            if (!Regex.IsMatch(cleaned, @"^[A-Z]{2}[\w\d]{2,12}$"))
                return "VAT number format is invalid for the specified country";
        }

        return string.Empty;
    }

    /// <summary>
    /// Formats VAT number for display
    /// </summary>
    public string FormatVatNumber(string? vat)
    {
        if (string.IsNullOrWhiteSpace(vat)) return string.Empty;

        var cleaned = vat.Replace(" ", "").Replace("-", "").ToUpperInvariant();

        // Format UK VAT numbers: GB 123 456 789
        if (cleaned.StartsWith("GB") && cleaned.Length >= 11)
        {
            var countryCode = cleaned.Substring(0, 2);
            var digits = cleaned.Substring(2);
            
            if (digits.Length == 9)
            {
                return $"{countryCode} {digits.Substring(0, 3)} {digits.Substring(3, 3)} {digits.Substring(6, 3)}";
            }
        }

        return cleaned;
    }
}
