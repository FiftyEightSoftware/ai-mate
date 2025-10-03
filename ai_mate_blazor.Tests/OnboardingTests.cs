using Bunit;
using Xunit;
using ai_mate_blazor.Pages;
using ai_mate_blazor.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

namespace ai_mate_blazor.Tests;

public class OnboardingTests : TestBase
{
    private TestContext CreateTestContext()
    {
        var ctx = new TestContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddScoped<EncryptionService>();
        ctx.Services.AddScoped<VoiceStorageService>();
        ctx.Services.AddScoped(sp => new HttpClient());
        ctx.Services.AddScoped<HmrcValidationService>();
        return ctx;
    }

    [Fact]
    public void Onboarding_RendersCorrectly()
    {
        // Arrange
        var ctx = CreateTestContext();

        // Act
        var cut = ctx.RenderComponent<Onboarding>();

        // Assert
        Assert.Contains("Welcome to AI Mate", cut.Markup);
        Assert.Contains("VAT Registration Number", cut.Markup);
        Assert.Contains("Government Gateway HMRC ID", cut.Markup);
        Assert.Contains("Continue", cut.Markup);
        Assert.Contains("Skip for now", cut.Markup);
    }

    [Fact]
    public void Onboarding_HasInputFields()
    {
        // Arrange
        var ctx = CreateTestContext();

        // Act
        var cut = ctx.RenderComponent<Onboarding>();

        // Assert
        var vatInput = cut.Find("input#vat-input");
        Assert.NotNull(vatInput);
        
        var hmrcInput = cut.Find("input#hmrc-input");
        Assert.NotNull(hmrcInput);
    }

    [Fact]
    public void Onboarding_HasActionButtons()
    {
        // Arrange
        var ctx = CreateTestContext();

        // Act
        var cut = ctx.RenderComponent<Onboarding>();

        // Assert
        var buttons = cut.FindAll("button");
        Assert.True(buttons.Count >= 2, "Should have at least Save and Skip buttons");
    }

    [Fact]
    public void Onboarding_HasAccessibilityLabels()
    {
        // Arrange
        var ctx = CreateTestContext();

        // Act
        var cut = ctx.RenderComponent<Onboarding>();

        // Assert
        var vatInput = cut.Find("input#vat-input");
        Assert.NotNull(vatInput.GetAttribute("aria-describedby"));
        Assert.Equal("false", vatInput.GetAttribute("aria-required"));
        
        var hmrcInput = cut.Find("input#hmrc-input");
        Assert.NotNull(hmrcInput.GetAttribute("aria-describedby"));
    }
}
