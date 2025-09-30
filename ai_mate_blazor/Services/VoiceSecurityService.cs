using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace ai_mate_blazor.Services;

public class VoiceSecurityService
{
    private readonly IJSRuntime _js;
    private readonly VoiceStorageService _storage;
    private readonly HttpClient _http;
    public VoiceSecurityService(IJSRuntime js, VoiceStorageService storage, HttpClient http)
    {
        _js = js; _storage = storage; _http = http;
    }

    public async Task<bool> GetEnforceAsync() => await _storage.GetVoiceEnforceAsync();
    public async Task SetEnforceAsync(bool enforce) => await _storage.SetVoiceEnforceAsync(enforce);

    public async Task<bool> IsEnrolledAsync()
    {
        var samples = await _storage.GetVoiceEnrollmentAsync();
        return samples.Count > 0;
    }

    public async Task<bool> EnrollAsync(int seconds = 3)
    {
        // Capture one sample and store; repeat enrollment by calling multiple times
        var dataUrl = await _js.InvokeAsync<string>("voice.recordAudio", seconds);
        if (string.IsNullOrWhiteSpace(dataUrl)) return false;
        // Try backend first
        try
        {
            var resp = await _http.PostAsJsonAsync("/voice/enroll", new { dataUrl });
            if (resp.IsSuccessStatusCode) return true;
        }
        catch { }
        // Fallback: local storage only
        try
        {
            var list = await _storage.GetVoiceEnrollmentAsync();
            list.Add(dataUrl);
            await _storage.SetVoiceEnrollmentAsync(list);
            return true;
        }
        catch { return false; }
    }

    public async Task ClearEnrollmentAsync()
    {
        await _storage.SetVoiceEnrollmentAsync(new List<string>());
    }

    public async Task<bool> VerifyAsync(int seconds = 2, CancellationToken ct = default)
    {
        var enforce = await _storage.GetVoiceEnforceAsync();
        if (!enforce) return true; // not enforced
        var enrolled = await _storage.GetVoiceEnrollmentAsync();
        if (enrolled.Count == 0) return false; // enforced but not enrolled

        // Capture a short sample
        var dataUrl = await _js.InvokeAsync<string>("voice.recordAudio", seconds);
        if (string.IsNullOrWhiteSpace(dataUrl)) return false;

        // Try backend verification
        try
        {
            var resp = await _http.PostAsJsonAsync("/voice/verify", new { dataUrl }, ct);
            if (resp.IsSuccessStatusCode)
            {
                var vr = await resp.Content.ReadFromJsonAsync<VerifyResponse>(cancellationToken: ct);
                if (vr is not null) return vr.ok;
            }
        }
        catch { }

        // Fallback: naive local check
        try
        {
            var len = dataUrl.Length;
            foreach (var s in enrolled)
            {
                var diff = Math.Abs(s.Length - len);
                if (diff < 5000) return true;
            }
        }
        catch { }
        return false;
    }

    private record VerifyResponse(bool ok, double score);
}
