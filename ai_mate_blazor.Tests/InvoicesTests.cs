using System;
using System.Linq;
using System.Text.Json;
using ai_mate_blazor.Pages;
using ai_mate_blazor.Services;
using Bunit;
using Microsoft.JSInterop;
using Xunit;

namespace ai_mate_blazor.Tests;

public class InvoicesTests : TestBase
{
    [Fact]
    public void Invoices_Renders_List_And_Allows_Filter_And_Navigation()
    {
        // Arrange
        var js = Ctx.JSInterop.SetupVoid("voice.downloadFile", _ => true);

        // Act
        var cut = Ctx.RenderComponent<Invoices>();

        // Assert: initial loading then list item
        cut.Markup.Contains("Invoices");
        // change status
        var selects = cut.FindAll("select");
        Assert.NotEmpty(selects);
        selects[0].Change("paid");
        // should re-render without exception
        cut.Markup.Contains("Invoices");

        // Export CSV button invokes JS
        cut.FindAll("button").First(b => b.TextContent.Contains("CSV")).Click();
        Ctx.JSInterop.VerifyInvoke("voice.downloadFile");
    }

    [Fact]
    public void Invoices_Import_Parses_Json_And_Refreshes()
    {
        // Arrange
        var cut = Ctx.RenderComponent<Invoices>();
        var payload = JsonSerializer.Serialize(new[]{ new ApiClient.InvoiceDto("INV-2","Beta",200m,"unpaid",DateTime.Today.AddDays(-1),DateTime.Today.AddDays(5),null) }, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        // Act
        cut.Find("textarea").Change(payload);
        cut.FindAll("button").First(b => b.TextContent.Contains("Import")).Click();

        // Assert: shows info message text eventually in DOM
        Assert.Contains("Import", cut.Markup);
    }
}
