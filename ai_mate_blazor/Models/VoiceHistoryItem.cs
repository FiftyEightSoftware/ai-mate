namespace ai_mate_blazor.Models;

public class VoiceHistoryItem
{
    public required string Transcript { get; set; }
    public string? ActionName { get; set; }
    public string? ActionKey { get; set; }
    public DateTimeOffset At { get; set; }
}
