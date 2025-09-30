namespace ai_mate_blazor.Models;

public class VoicePrompt
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    // A human-friendly name for the action this prompt triggers (e.g., "Create Invoice")
    public string? ActionName { get; set; }
    // A stable action key used for routing in code (e.g., "create_invoice")
    public string? ActionKey { get; set; }
    // The phrase text captured from the user's voice (or typed)
    public string? Phrase { get; set; }
    // If true, this prompt will be ignored during voice matching
    public bool Hidden { get; set; }
    // If true, this action requires extra security checks (passphrase / confirmation)
    public bool Privileged { get; set; }
}
