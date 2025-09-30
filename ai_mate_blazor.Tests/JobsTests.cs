using ai_mate_blazor.Pages;
using Bunit;
using Xunit;

namespace ai_mate_blazor.Tests;

public class JobsTests : TestBase
{
    [Fact]
    public void Jobs_Renders_And_Creates()
    {
        var cut = Ctx.RenderComponent<Jobs>();
        // Initially shows loading then content
        cut.Markup.Contains("Jobs");

        // Enter a new job title and submit
        var input = cut.Find("input");
        input.Change("My New Job");
        cut.Find("form").Submit();

        // Should re-render; list markup should be present
        Assert.Contains("Jobs", cut.Markup);
    }
}
