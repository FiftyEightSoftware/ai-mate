using System.Linq;
using ai_mate_blazor.Pages;
using Bunit;
using Xunit;

namespace ai_mate_blazor.Tests;

public class SettingsTests : TestBase
{
    [Fact]
    public void Settings_Renders_And_Clicks_Save_Buttons()
    {
        var cut = Ctx.RenderComponent<Settings>();
        Assert.Contains("Settings", cut.Markup);

        // Click a few buttons to ensure no exceptions and UI updates
        var buttons = cut.FindAll("button");
        Assert.True(buttons.Count > 0);
        // Save history max (if present)
        var saveHist = buttons.FirstOrDefault(b => b.TextContent.Contains("Save"));
        saveHist?.Click();

        // Toggle toasts -> ensure click path executes without exceptions
        Ctx.JSInterop.SetupVoid("localStorage.setItem", _ => true);
        buttons = cut.FindAll("button");
        var toastsSave = buttons.FirstOrDefault(b => b.TextContent.Contains("Save") && b.ParentElement!.TextContent.Contains("Toasts"));
        toastsSave?.Click();

        // Export button triggers downloadFile via JS
        Ctx.JSInterop.SetupVoid("voice.downloadFile", _ => true);
        buttons = cut.FindAll("button");
        var export = buttons.FirstOrDefault(b => b.TextContent.Contains("Export"));
        export?.Click();

        // Import button uses local storage only; ensure it doesn't throw
        var import = buttons.FirstOrDefault(b => b.TextContent.Contains("Import"));
        import?.Click();

        Assert.Contains("Settings", cut.Markup);
    }
}
