using ai_mate_blazor.Pages;
using Bunit;
using Xunit;

namespace ai_mate_blazor.Tests;

public class WeatherTests : TestBase
{
    [Fact]
    public void Weather_Renders_Table()
    {
        var cut = Ctx.RenderComponent<Weather>();
        // Should load from stubbed /sample-data/weather.json and render table
        Assert.Contains("Weather", cut.Markup);
        Assert.Contains("Temp. (C)", cut.Markup);
        Assert.Contains("Warm", cut.Markup);
    }
}
