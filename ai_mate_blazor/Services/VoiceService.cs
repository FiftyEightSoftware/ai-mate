using System.Text.RegularExpressions;
using ai_mate_blazor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ai_mate_blazor.Services;

public class VoiceService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly NavigationManager _nav;
    private readonly VoiceStorageService _storage;
    private readonly ApiClient _api;
    private readonly VoiceSecurityService _security;

    private DotNetObjectReference<VoiceService>? _dotnetRef;
    private IJSObjectReference? _module;

    public bool IsListening { get; private set; }
    public string? LastTranscript { get; private set; }
    public string? LastMatchedAction { get; private set; }
    public event Action? FeedbackChanged;
    public event Action? PendingChanged;
    public sealed class HistoryItem
    {
        public required string Transcript { get; init; }
        public string? ActionName { get; init; }
        public DateTimeOffset At { get; init; } = DateTimeOffset.Now;
        public string? ActionKey { get; init; }
    }
    private readonly List<HistoryItem> _history = new();
    private int _historyMax = 10;
    public IReadOnlyList<HistoryItem> History => _history;
    public int HistoryMax => _historyMax;

    // Security controls
    private string? _passphrase;
    private HashSet<string> _privilegedKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<DateTimeOffset> _execTimestamps = new();
    private DateTimeOffset? _lastVerifiedAt;
    private static readonly TimeSpan VerifyCacheFor = TimeSpan.FromMinutes(5);
    private const int RateLimitCount = 3;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromSeconds(10);
    private readonly HashSet<string> _sessionAllowedActions = new(StringComparer.OrdinalIgnoreCase);

    // Pending confirmation
    public string? PendingActionKey { get; private set; }
    public string? PendingActionName { get; private set; }
    public string? PendingReason { get; private set; }

    public VoiceService(IJSRuntime js, NavigationManager nav, VoiceStorageService storage, ApiClient api, VoiceSecurityService security)
    {
        _js = js;
        _nav = nav;
        _storage = storage;
        _api = api;
        _security = security;
    }

    public async Task InitializeAsync()
    {
        // Ensure voice.js is loaded (it is included via index.html). This keeps API surface clear.
        try
        {
            // no-op to check localStorage access works
            await _js.InvokeVoidAsync("eval", "void 0");
        }
        catch { }

        // Load persisted history and max size
        try
        {
            var persisted = await _storage.GetHistoryAsync();
            _history.Clear();
            foreach (var h in persisted)
            {
                _history.Add(new HistoryItem { Transcript = h.Transcript, ActionName = h.ActionName, ActionKey = h.ActionKey, At = h.At });
            }
            _historyMax = await _storage.GetHistoryMaxAsync(10);
            while (_history.Count > _historyMax) _history.RemoveAt(_history.Count - 1);
            _passphrase = await _storage.GetPassphraseAsync();
            try
            {
                var prompts = await _storage.GetAsync();
                _privilegedKeys = prompts.Where(p => p.Privileged && !string.IsNullOrWhiteSpace(p.ActionKey))
                    .Select(p => p.ActionKey!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                // Auto-privilege create_* if setting is enabled
                if (await _storage.GetAutoPrivCreateAsync(true))
                {
                    foreach (var p in prompts)
                    {
                        if (!string.IsNullOrWhiteSpace(p.ActionKey) && p.ActionKey.StartsWith("create_", StringComparison.OrdinalIgnoreCase))
                        {
                            _privilegedKeys.Add(p.ActionKey);
                        }
                    }
                }
            }
            catch { _privilegedKeys = new(StringComparer.OrdinalIgnoreCase); }
        }
        catch { }
    }

    public async Task<bool> StartAsync()
    {
        if (IsListening) return true;
        _dotnetRef ??= DotNetObjectReference.Create(this);
        var ok = await _js.InvokeAsync<bool>("voice.startListening", _dotnetRef);
        IsListening = ok;
        return ok;
    }

    public async Task StopAsync()
    {
        if (!IsListening) return;
        await _js.InvokeVoidAsync("voice.stopListening");
        IsListening = false;
    }

    // Called from JS via DotNetObjectReference
    [JSInvokable]
    public async Task OnRecognized(string transcript)
    {
        var text = Normalize(transcript);
        if (string.IsNullOrWhiteSpace(text)) return;
        var prompts = await _storage.GetAsync();
        var match = MatchPrompt(text, prompts);
        LastTranscript = transcript;
        LastMatchedAction = match?.ActionName;
        _history.Insert(0, new HistoryItem { Transcript = transcript, ActionName = match?.ActionName, ActionKey = match?.ActionKey, At = DateTimeOffset.Now });
        while (_history.Count > _historyMax) _history.RemoveAt(_history.Count - 1);
        await PersistHistoryAsync();
        FeedbackChanged?.Invoke();
        if (match is null) return;
        if (!await CanExecuteAsync(match.ActionKey ?? string.Empty, match.ActionName ?? string.Empty, text)) return;
        await ExecuteActionAsync(match.ActionKey ?? string.Empty);
    }

    private static string Normalize(string s)
    {
        s = s.ToLowerInvariant().Trim();
        s = Regex.Replace(s, "\\s+", " ");
        return s;
    }

    private static VoicePrompt? MatchPrompt(string heard, List<VoicePrompt> prompts)
    {
        foreach (var p in prompts)
        {
            if (p.Hidden) continue;
            var phrase = Normalize(p.Phrase ?? string.Empty);
            if (string.IsNullOrWhiteSpace(phrase)) continue;
            // Basic contains or equality match
            if (heard.Contains(phrase, StringComparison.Ordinal) || heard.Equals(phrase, StringComparison.Ordinal))
            {
                return p;
            }
        }
        return null;
    }

    private async Task ExecuteActionAsync(string actionKey)
    {
        switch (actionKey)
        {
            case "create_invoice":
                _nav.NavigateTo("/invoices");
                break;
            case "show_payments":
                _nav.NavigateTo("/expenses");
                break;
            case "show_jobs":
                _nav.NavigateTo("/jobs");
                break;
            case "create_job":
                var created = await _api.CreateJobAsync("Untitled (Voice)");
                _nav.NavigateTo("/jobs");
                break;
            case "show_quotes":
                _nav.NavigateTo("/quotes");
                break;
            case "show_clients":
                _nav.NavigateTo("/clients");
                break;
            case "create_quote":
                _nav.NavigateTo("/quotes");
                break;
            case "show_invoices":
                _nav.NavigateTo("/invoices");
                break;
            case "show_expenses":
                _nav.NavigateTo("/expenses");
                break;
            case "show_assistant":
                _nav.NavigateTo("/assistant");
                break;
            case "show_settings":
                _nav.NavigateTo("/settings");
                break;
            default:
                // Unknown action - no-op
                break;
        }

        // Toast for privileged actions (informational)
        try
        {
            if (_privilegedKeys.Contains(actionKey) && await _storage.GetToastsEnabledAsync(true))
            {
                await _js.InvokeVoidAsync("voice.toast", "Privileged action executed");
            }
        }
        catch { }
    }

    // Simulate a recognition event from UI
    public async Task SimulateRecognitionAsync(string transcript, bool execute = false)
    {
        var text = Normalize(transcript ?? string.Empty);
        if (string.IsNullOrWhiteSpace(text)) return;
        var prompts = await _storage.GetAsync();
        var match = MatchPrompt(text, prompts);
        LastTranscript = transcript;
        LastMatchedAction = match?.ActionName;
        _history.Insert(0, new HistoryItem { Transcript = transcript, ActionName = match?.ActionName, ActionKey = match?.ActionKey, At = DateTimeOffset.Now });
        while (_history.Count > _historyMax) _history.RemoveAt(_history.Count - 1);
        await PersistHistoryAsync();
        FeedbackChanged?.Invoke();
        if (execute && match is not null)
        {
            if (!await CanExecuteAsync(match.ActionKey ?? string.Empty, match.ActionName ?? string.Empty, text)) return;
            await ExecuteActionAsync(match.ActionKey ?? string.Empty);
        }
    }

    public async Task RunActionAsync(string? actionKey)
    {
        if (string.IsNullOrWhiteSpace(actionKey)) return;
        if (!await CanExecuteAsync(actionKey!, actionKey!, Normalize(LastTranscript ?? string.Empty))) return;
        await ExecuteActionAsync(actionKey!);
    }

    public void ClearHistory()
    {
        _history.Clear();
        _ = PersistHistoryAsync();
        FeedbackChanged?.Invoke();
    }

    public void ClearFeedback()
    {
        LastTranscript = null;
        LastMatchedAction = null;
        FeedbackChanged?.Invoke();
    }

    private bool CheckRateLimit()
    {
        var now = DateTimeOffset.Now;
        _execTimestamps.Enqueue(now);
        while (_execTimestamps.Count > 0 && now - _execTimestamps.Peek() > RateLimitWindow) _execTimestamps.Dequeue();
        return _execTimestamps.Count <= RateLimitCount;
    }

    private async Task<bool> CanExecuteAsync(string actionKey, string actionName, string normalizedTranscript)
    {
        // Rate limiting
        if (!CheckRateLimit())
        {
            PendingActionKey = actionKey;
            PendingActionName = actionName;
            PendingReason = "Rate limited. Confirm to proceed.";
            PendingChanged?.Invoke();
            return false;
        }

        // Verification cache and enforcement
        if (await _security.GetEnforceAsync())
        {
            var now = DateTimeOffset.Now;
            if (!(_lastVerifiedAt.HasValue && (now - _lastVerifiedAt.Value) < VerifyCacheFor))
            {
                var ok = await _security.VerifyAsync();
                if (!ok)
                {
                    PendingActionKey = actionKey;
                    PendingActionName = actionName;
                    PendingReason = "Voice verification failed. Confirm to proceed.";
                    PendingChanged?.Invoke();
                    return false;
                }
                _lastVerifiedAt = now;
            }
        }

        // Passphrase for privileged actions
        if (_privilegedKeys.Contains(actionKey))
        {
            if (_sessionAllowedActions.Contains(actionKey)) return true; // passphrase remembered for session
            var pp = Normalize(_passphrase ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(pp) && !normalizedTranscript.Contains(pp, StringComparison.Ordinal))
            {
                PendingActionKey = actionKey;
                PendingActionName = actionName;
                PendingReason = "Missing security passphrase. Confirm to proceed.";
                PendingChanged?.Invoke();
                return false;
            }
        }

        return true;
    }

    public async Task ConfirmPendingAsync(bool extendVerifyWindow = false, bool rememberActionForSession = false)
    {
        if (string.IsNullOrWhiteSpace(PendingActionKey)) return;
        var key = PendingActionKey!;
        if (rememberActionForSession) _sessionAllowedActions.Add(key);
        if (extendVerifyWindow) _lastVerifiedAt = DateTimeOffset.Now;
        PendingActionKey = null; PendingActionName = null; PendingReason = null;
        PendingChanged?.Invoke();
        await ExecuteActionAsync(key);
    }

    public void CancelPending()
    {
        PendingActionKey = null; PendingActionName = null; PendingReason = null;
        PendingChanged?.Invoke();
    }

    public bool IsPrivileged(string? actionKey)
    {
        if (string.IsNullOrWhiteSpace(actionKey)) return false;
        return _privilegedKeys.Contains(actionKey);
    }

    public async Task SetHistoryMaxAsync(int value)
    {
        var clamped = Math.Clamp(value, 0, 50);
        _historyMax = clamped;
        while (_history.Count > _historyMax) _history.RemoveAt(_history.Count - 1);
        await _storage.SetHistoryMaxAsync(_historyMax);
        await PersistHistoryAsync();
        FeedbackChanged?.Invoke();
    }

    private async Task PersistHistoryAsync()
    {
        try
        {
            var items = _history.ConvertAll(h => new VoiceHistoryItem
            {
                Transcript = h.Transcript,
                ActionName = h.ActionName,
                ActionKey = h.ActionKey,
                At = h.At
            });
            await _storage.SaveHistoryAsync(items);
        }
        catch { }
    }

    public async ValueTask DisposeAsync()
    {
        try { await StopAsync(); } catch { }
        _dotnetRef?.Dispose();
        if (_module is not null)
        {
            try { await _module.DisposeAsync(); } catch { }
        }
    }
}
