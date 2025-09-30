using ai_mate_blazor.Pages;
using Bunit;
using Xunit;

namespace ai_mate_blazor.Tests;

public class HomeTests : TestBase
{
    [Fact]
    public void Home_Renders_Cards()
    {
        var cut = Ctx.RenderComponent<Home>();
        Assert.Contains("Home", cut.Markup);
        // Should have several NavLinks to key pages
        Assert.Contains("/dashboard", cut.Markup);
        Assert.Contains("/jobs", cut.Markup);
        Assert.Contains("/invoices", cut.Markup);
        Assert.Contains("/settings", cut.Markup);
    }
}
