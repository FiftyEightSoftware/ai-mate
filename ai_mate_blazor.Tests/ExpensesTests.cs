using ai_mate_blazor.Pages;
using Bunit;
using Xunit;

namespace ai_mate_blazor.Tests;

public class ExpensesTests : TestBase
{
    [Fact]
    public void Expenses_Renders_Empty()
    {
        var cut = Ctx.RenderComponent<Expenses>();
        Assert.Contains("Expenses", cut.Markup);
        Assert.Contains("No expenses yet", cut.Markup);
    }
}
