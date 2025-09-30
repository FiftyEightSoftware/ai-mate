using ai_mate_blazor.Pages;
using Bunit;
using Xunit;

namespace ai_mate_blazor.Tests;

public class ClientsTests : TestBase
{
    [Fact]
    public void Clients_Renders_List()
    {
        var cut = Ctx.RenderComponent<Clients>();
        Assert.Contains("Clients", cut.Markup);
        var items = cut.FindAll(".list-item");
        Assert.Equal(3, items.Count);
    }
}
