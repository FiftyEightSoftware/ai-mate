using System.Text.Json;
using ai_mate_blazor.Models;
using Microsoft.JSInterop;

namespace ai_mate_blazor.Services;

public class VoiceStorageService
{
    private readonly IJSRuntime _js;
    private readonly EncryptionService _encryption;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
    public VoiceStorageService(IJSRuntime js, EncryptionService encryption) 
    { 
        _js = js; 
        _encryption = encryption;
    }
    private const string PromptsKey = "aimate_voice_prompts_v1";
    private const string HistoryKey = "aimate_voice_history_v1";
    private const string HistoryMaxKey = "aimate_voice_history_max_v1";
    private const string ToastsKey = "aimate_voice_toasts_enabled_v1";
    private const string VoiceEnforceKey = "aimate_voice_enforce_v1";
    private const string VoiceEnrollKey = "aimate_voice_enroll_v1"; // JSON string[] of data URLs
    private const string PassphraseKey = "aimate_voice_passphrase_v1";
    private const string AutoPrivCreateKey = "aimate_voice_autopriv_create_v1";
    private const string VatRegistrationIdKey = "aimate_vat_registration_id_v1";
    private const string HmrcGatewayIdKey = "aimate_hmrc_gateway_id_v1";
    private const string OnboardingCompletedKey = "aimate_onboarding_completed_v1";
    private const string BusinessProfilesKey = "aimate_business_profiles_v1";
    private const string ActiveProfileIdKey = "aimate_active_profile_id_v1";
    private const string TaxAuditKey = "aimate_tax_audit_v1";

    public async Task<List<VoicePrompt>> GetAsync()
    {
        try
        {
            // Prefer JS helper to keep a single source of truth
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", PromptsKey);
            if (string.IsNullOrWhiteSpace(json)) return new();
            return JsonSerializer.Deserialize<List<VoicePrompt>>(json!, JsonOpts) ?? new();
        }
        catch { return new(); }
    }

    public async Task SaveAsync(List<VoicePrompt> prompts)
    {
        var json = JsonSerializer.Serialize(prompts ?? new(), JsonOpts);
        await _js.InvokeVoidAsync("localStorage.setItem", PromptsKey, json);
    }

    public async Task<List<VoiceHistoryItem>> GetHistoryAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", HistoryKey);
            if (string.IsNullOrWhiteSpace(json)) return new();
            return JsonSerializer.Deserialize<List<VoiceHistoryItem>>(json!, JsonOpts) ?? new();
        }
        catch { return new(); }
    }

    public async Task SaveHistoryAsync(List<VoiceHistoryItem> items)
    {
        var json = JsonSerializer.Serialize(items ?? new(), JsonOpts);
        await _js.InvokeVoidAsync("localStorage.setItem", HistoryKey, json);
    }

    public async Task<int> GetHistoryMaxAsync(int fallback = 10)
    {
        try
        {
            var v = await _js.InvokeAsync<string?>("localStorage.getItem", HistoryMaxKey);
            if (int.TryParse(v, out var i)) return Math.Clamp(i, 0, 50);
            return fallback;
        }
        catch { return fallback; }
    }

    public async Task SetHistoryMaxAsync(int value)
    {
        var clamped = Math.Clamp(value, 0, 50);
        await _js.InvokeVoidAsync("localStorage.setItem", HistoryMaxKey, clamped.ToString());
    }

    public async Task<bool> GetToastsEnabledAsync(bool fallback = true)
    {
        try
        {
            var v = await _js.InvokeAsync<string?>("localStorage.getItem", ToastsKey);
            if (string.IsNullOrWhiteSpace(v)) return fallback;
            return v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        catch { return fallback; }
    }

    public async Task SetToastsEnabledAsync(bool enabled)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", ToastsKey, enabled ? "1" : "0");
    }

    // Voice security: enforce flag
    public async Task<bool> GetVoiceEnforceAsync(bool fallback = false)
    {
        try
        {
            var v = await _js.InvokeAsync<string?>("localStorage.getItem", VoiceEnforceKey);
            if (string.IsNullOrWhiteSpace(v)) return fallback;
            return v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        catch { return fallback; }
    }

    public async Task SetVoiceEnforceAsync(bool enforce)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", VoiceEnforceKey, enforce ? "1" : "0");
    }

    // Voice security: enrollment samples
    public async Task<List<string>> GetVoiceEnrollmentAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", VoiceEnrollKey);
            if (string.IsNullOrWhiteSpace(json)) return new();
            return JsonSerializer.Deserialize<List<string>>(json!, JsonOpts) ?? new();
        }
        catch { return new(); }
    }

    public async Task SetVoiceEnrollmentAsync(List<string> samples)
    {
        var json = JsonSerializer.Serialize(samples ?? new(), JsonOpts);
        await _js.InvokeVoidAsync("localStorage.setItem", VoiceEnrollKey, json);
    }

    // Security passphrase
    public async Task<string?> GetPassphraseAsync()
    {
        try { return await _js.InvokeAsync<string?>("localStorage.getItem", PassphraseKey); }
        catch { return null; }
    }
    public async Task SetPassphraseAsync(string? value)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", PassphraseKey, value ?? "");
    }

    // Auto privilege: mark create_* actions privileged by default
    public async Task<bool> GetAutoPrivCreateAsync(bool fallback = true)
    {
        try
        {
            var v = await _js.InvokeAsync<string?>("localStorage.getItem", AutoPrivCreateKey);
            if (string.IsNullOrWhiteSpace(v)) return fallback;
            return v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        catch { return fallback; }
    }

    public async Task SetAutoPrivCreateAsync(bool enabled)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", AutoPrivCreateKey, enabled ? "1" : "0");
    }

    // Export and Import combined data for backup/restore
    private sealed class ExportModel
    {
        public string Version { get; set; } = "2";
        public List<VoicePrompt> Prompts { get; set; } = new();
        public List<VoiceHistoryItem> History { get; set; } = new();
        public int HistoryMax { get; set; } = 10;
        public List<BusinessProfile> BusinessProfiles { get; set; } = new();
        public string? ActiveProfileId { get; set; }
    }

    public async Task<string> ExportAllAsync()
    {
        var prompts = await GetAsync();
        var history = await GetHistoryAsync();
        var max = await GetHistoryMaxAsync(10);
        var profiles = await GetBusinessProfilesAsync();
        var activeId = await GetActiveProfileIdAsync();
        var model = new ExportModel 
        { 
            Prompts = prompts, 
            History = history, 
            HistoryMax = max,
            BusinessProfiles = profiles,
            ActiveProfileId = activeId
        };
        return JsonSerializer.Serialize(model, JsonOpts);
    }

    public async Task ImportAllAsync(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            var model = JsonSerializer.Deserialize<ExportModel>(json, JsonOpts);
            if (model is null) return;
            await SaveAsync(model.Prompts ?? new());
            await SaveHistoryAsync(model.History ?? new());
            await SetHistoryMaxAsync(model.HistoryMax);
            
            // Import business profiles if available (version 2+)
            if (model.BusinessProfiles?.Count > 0)
            {
                await SaveBusinessProfilesAsync(model.BusinessProfiles);
                if (!string.IsNullOrWhiteSpace(model.ActiveProfileId))
                {
                    await SetActiveProfileIdAsync(model.ActiveProfileId);
                }
            }
        }
        catch { }
    }

    // VAT Registration ID (encrypted)
    public async Task<string?> GetVatRegistrationIdAsync()
    {
        try 
        { 
            var encrypted = await _js.InvokeAsync<string?>("localStorage.getItem", VatRegistrationIdKey);
            return _encryption.Decrypt(encrypted);
        }
        catch { return null; }
    }

    public async Task SetVatRegistrationIdAsync(string? value)
    {
        var encrypted = _encryption.Encrypt(value);
        await _js.InvokeVoidAsync("localStorage.setItem", VatRegistrationIdKey, encrypted);
    }

    // HMRC Gateway ID (encrypted)
    public async Task<string?> GetHmrcGatewayIdAsync()
    {
        try 
        { 
            var encrypted = await _js.InvokeAsync<string?>("localStorage.getItem", HmrcGatewayIdKey);
            return _encryption.Decrypt(encrypted);
        }
        catch { return null; }
    }

    public async Task SetHmrcGatewayIdAsync(string? value)
    {
        var encrypted = _encryption.Encrypt(value);
        await _js.InvokeVoidAsync("localStorage.setItem", HmrcGatewayIdKey, encrypted);
    }

    // Onboarding status
    public async Task<bool> GetOnboardingCompletedAsync(bool fallback = false)
    {
        try
        {
            var v = await _js.InvokeAsync<string?>("localStorage.getItem", OnboardingCompletedKey);
            if (string.IsNullOrWhiteSpace(v)) return fallback;
            return v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        catch { return fallback; }
    }

    public async Task SetOnboardingCompletedAsync(bool completed)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", OnboardingCompletedKey, completed ? "1" : "0");
    }

    // Business Profiles
    public async Task<List<BusinessProfile>> GetBusinessProfilesAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", BusinessProfilesKey);
            if (string.IsNullOrWhiteSpace(json)) return new();
            return JsonSerializer.Deserialize<List<BusinessProfile>>(json!, JsonOpts) ?? new();
        }
        catch { return new(); }
    }

    public async Task SaveBusinessProfilesAsync(List<BusinessProfile> profiles)
    {
        var json = JsonSerializer.Serialize(profiles ?? new(), JsonOpts);
        await _js.InvokeVoidAsync("localStorage.setItem", BusinessProfilesKey, json);
    }

    public async Task<string?> GetActiveProfileIdAsync()
    {
        try { return await _js.InvokeAsync<string?>("localStorage.getItem", ActiveProfileIdKey); }
        catch { return null; }
    }

    public async Task SetActiveProfileIdAsync(string? profileId)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", ActiveProfileIdKey, profileId ?? "");
    }

    public async Task<BusinessProfile?> GetActiveProfileAsync()
    {
        var profiles = await GetBusinessProfilesAsync();
        if (profiles.Count == 0) return null;
        
        var activeId = await GetActiveProfileIdAsync();
        return profiles.FirstOrDefault(p => p.Id == activeId) ?? profiles.First();
    }

    // Tax Audit Trail
    public async Task<List<TaxAuditEntry>> GetTaxAuditAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", TaxAuditKey);
            if (string.IsNullOrWhiteSpace(json)) return new();
            return JsonSerializer.Deserialize<List<TaxAuditEntry>>(json!, JsonOpts) ?? new();
        }
        catch { return new(); }
    }

    public async Task AppendTaxAuditEntryAsync(TaxAuditEntry entry)
    {
        try
        {
            var audit = await GetTaxAuditAsync();
            audit.Add(entry);
            
            // Keep only last 100 entries
            if (audit.Count > 100)
            {
                audit = audit.OrderByDescending(a => a.Timestamp).Take(100).ToList();
            }
            
            var json = JsonSerializer.Serialize(audit, JsonOpts);
            await _js.InvokeVoidAsync("localStorage.setItem", TaxAuditKey, json);
        }
        catch { }
    }

    // Save tax details with audit trail
    public async Task SaveTaxDetailsWithAuditAsync(string? vat, string? hmrc, string? profileId = null)
    {
        var oldVat = await GetVatRegistrationIdAsync();
        var oldHmrc = await GetHmrcGatewayIdAsync();
        
        var audit = new TaxAuditEntry
        {
            Action = "Update Tax Details",
            ProfileId = profileId,
            VatChanged = vat != oldVat,
            HmrcChanged = hmrc != oldHmrc,
            OldVat = oldVat,
            NewVat = vat,
            OldHmrc = oldHmrc,
            NewHmrc = hmrc
        };
        
        await SetVatRegistrationIdAsync(vat);
        await SetHmrcGatewayIdAsync(hmrc);
        
        if (audit.VatChanged || audit.HmrcChanged)
        {
            await AppendTaxAuditEntryAsync(audit);
        }
    }
}
