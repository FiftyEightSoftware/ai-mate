using ai_mate_blazor.Pages;
using Bunit;
using Xunit;

namespace ai_mate_blazor.Tests;

public class DashboardTests : TestBase
{
    [Fact]
    public void Dashboard_Renders_WithoutErrors()
    {
        // Arrange
        var js = Ctx.JSInterop.SetupVoid("voice.downloadFile", _ => true);

        // Act
        var cut = Ctx.RenderComponent<Dashboard>();

        // Assert
        cut.MarkupMatches(cut.Markup); // render succeeded
    }
}
