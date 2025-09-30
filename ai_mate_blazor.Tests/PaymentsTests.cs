using System.Linq;
using ai_mate_blazor.Pages;
using Bunit;
using Xunit;

namespace ai_mate_blazor.Tests;

public class PaymentsTests : TestBase
{
    [Fact]
    public void Payments_Renders_Filter_And_Export()
    {
        var cut = Ctx.RenderComponent<Payments>();
        // Should render title and list or empty
        Assert.Contains("Payments", cut.Markup);

        // Click refresh and export
        var buttons = cut.FindAll("button");
        Assert.True(buttons.Count >= 2);
        buttons.First(b => b.TextContent.Contains("Refresh")).Click();
        Ctx.JSInterop.SetupVoid("voice.downloadFile", _ => true);
        buttons.First(b => b.TextContent.Contains("CSV")).Click();
    }
}
