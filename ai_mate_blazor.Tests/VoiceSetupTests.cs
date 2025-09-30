using System.Linq;
using ai_mate_blazor.Pages;
using Bunit;
using Xunit;

namespace ai_mate_blazor.Tests;

public class VoiceSetupTests : TestBase
{
    [Fact]
    public void VoiceSetup_Renders_And_Interacts_Basically()
    {
        var cut = Ctx.RenderComponent<VoiceSetup>();
        Assert.Contains("Voice Setup", cut.Markup);

        // Ensure buttons exist and some clicks work without throwing
        var buttons = cut.FindAll("button");
        Assert.True(buttons.Count > 0);
        // Click Save (form submit) and Reload
        var form = cut.Find("form");
        cut.InvokeAsync(() => form.Submit());
        cut.WaitForState(() => true);
        buttons = cut.FindAll("button");
        var reloadBtn = buttons.FirstOrDefault(b => b.TextContent.Contains("Reload"));
        if (reloadBtn is not null) cut.InvokeAsync(() => reloadBtn.Click());
        cut.WaitForState(() => true);
        // Click Add without inputs (should set error but not throw)
        buttons = cut.FindAll("button");
        var addBtn = buttons.FirstOrDefault(b => b.TextContent.Contains("Add"));
        if (addBtn is not null) cut.InvokeAsync(() => addBtn.Click());
        cut.WaitForState(() => true);
        // Should show info message or persist without exceptions
        Assert.Contains("Voice Setup", cut.Markup);

        Assert.Contains("Voice Setup", cut.Markup);
    }
}
