using System.Linq;
using ai_mate_blazor.Pages;
using Bunit;
using Xunit;

namespace ai_mate_blazor.Tests;

public class AssistantTests : TestBase
{
    [Fact]
    public void Assistant_SimulatePhrase_Submits()
    {
        var cut = Ctx.RenderComponent<Assistant>();
        Assert.Contains("Mate", cut.Markup);
        var input = cut.Find("input");
        input.Change("show jobs");
        var form = cut.Find("form");
        form.Submit();
        Assert.Contains("Mate", cut.Markup);
    }
}
