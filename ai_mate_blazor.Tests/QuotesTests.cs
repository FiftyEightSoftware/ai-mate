using ai_mate_blazor.Pages;
using Bunit;
using Xunit;

namespace ai_mate_blazor.Tests;

public class QuotesTests : TestBase
{
    [Fact]
    public void Quotes_Renders_Empty()
    {
        var cut = Ctx.RenderComponent<Quotes>();
        Assert.Contains("Quotes", cut.Markup);
        Assert.Contains("No quotes yet", cut.Markup);
    }
}
