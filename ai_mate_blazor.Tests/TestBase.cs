using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using ai_mate_blazor.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ai_mate_blazor.Tests;

public abstract class TestBase : IDisposable
{
    protected readonly TestContext Ctx = new();

    protected TestBase()
    {
        // BUnit's default JSRuntime supports expectation/verification
        // Allow loose JS interop to avoid strict failures for unconfigured calls
        Ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        // Common JS stubs
        Ctx.JSInterop.Setup<string?>("localStorage.getItem", _ => true).SetResult((string?)null);
        Ctx.JSInterop.SetupVoid("localStorage.setItem", _ => true);
        Ctx.JSInterop.SetupVoid("voice.downloadFile", _ => true);
        Ctx.JSInterop.Setup<string?>("voice.pickFileText", _ => true).SetResult(null);
        Ctx.JSInterop.Setup<string?>("voice.recordOnce", _ => true).SetResult("test");
        Ctx.JSInterop.Setup<bool>("voice.copyText", _ => true).SetResult(true);
        Ctx.JSInterop.SetupVoid("voice.prefillMenuSimulate", _ => true);
        Ctx.JSInterop.SetupVoid("voice.toast", _ => true);
        // Register ApiClient with a stub handler to return canned responses
        var handler = new StubHttpMessageHandler();
        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };
        var api = new ApiClient(http);
        Ctx.Services.AddSingleton(api);
        // Scoped HttpClient for components that inject HttpClient directly (e.g., Weather)
        Ctx.Services.AddScoped(sp => new HttpClient(new StubHttpMessageHandler()) { BaseAddress = new Uri("http://localhost") });
        // Register any other app services used by components (scoped to align with HttpClient)
        Ctx.Services.AddScoped<ai_mate_blazor.Services.VoiceStorageService>();
        Ctx.Services.AddScoped<ai_mate_blazor.Services.VoiceSecurityService>();
        Ctx.Services.AddScoped<ai_mate_blazor.Services.VoiceService>();
    }

    public void Dispose() => Ctx.Dispose();

    protected class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath + request.RequestUri!.Query;
            object? payload = null;
            // In-memory stores to simulate state across requests (static for test process)
            // Note: using simple static lists keyed by endpoint
            payload ??= null;
            // Dashboard
            if (path.StartsWith("/api/dashboard"))
            {
                payload = new
                {
                    outstandingTotal = 1234.56m,
                    overdueTotal = 200m,
                    dueSoonTotal = 150m,
                    paidLast30 = 500m,
                    invoices = new[]{ new { id = "INV-1", customer = "Acme", amount = 100m, dueDate = DateTime.Today.AddDays(3), status = "unpaid" } },
                    projectedCashFlow = new[]{ new { date = DateTime.Today.AddDays(7), amount = 300m } }
                };
            }
            // Single invoice
            else if (path.StartsWith("/api/invoices/") && request.Method == HttpMethod.Get)
            {
                payload = new { id = "INV-1", customer = "Acme", amount = 100m, status = "unpaid", issueDate = (DateTime?)DateTime.Today.AddDays(-2), dueDate = (DateTime?)DateTime.Today.AddDays(3), paidDate = (DateTime?)null };
            }
            // Invoices list
            else if (path == "/api/invoices" || (path.StartsWith("/api/invoices?") && request.Method == HttpMethod.Get))
            {
                payload = new[]{ new { id = "INV-1", customer = "Acme", amount = 100m, status = "unpaid", issueDate = (DateTime?)DateTime.Today.AddDays(-2), dueDate = (DateTime?)DateTime.Today.AddDays(3), paidDate = (DateTime?)null } };
            }
            // Import invoices
            else if (path == "/api/import/invoices" && request.Method == HttpMethod.Post)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
            // Mark paid
            else if (path.StartsWith("/api/invoices/") && path.EndsWith("/mark-paid") && request.Method == HttpMethod.Post)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
            // Payments
            else if (path.StartsWith("/api/payments") && request.Method == HttpMethod.Get)
            {
                payload = new[]{ new Dictionary<string, object?>{ ["id"] = "P-1", ["invoiceId"] = "INV-1", ["amount"] = 75m, ["paidAt"] = DateTime.Today.ToString("yyyy-MM-dd") } };
            }
            else if (path == "/api/payments" && request.Method == HttpMethod.Post)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }
            // Jobs list
            else if (path.StartsWith("/api/jobs") && request.Method == HttpMethod.Get)
            {
                payload = new[]{ new { id = Guid.NewGuid(), title = "Test Job", status = "Upcoming", quotedPrice = (decimal?)100m } };
            }
            // Create job
            else if (path == "/api/jobs" && request.Method == HttpMethod.Post)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { id = Guid.NewGuid(), title = "Created", status = "Upcoming", quotedPrice = (decimal?)null }, new JsonSerializerOptions(JsonSerializerDefaults.Web)), Encoding.UTF8, "application/json")
                });
            }
            // Health
            else if (path == "/api/health")
            {
                payload = new { ok = true };
            }
            // Static sample data for Weather
            else if (path == "/sample-data/weather.json" && request.Method == HttpMethod.Get)
            {
                var weather = new[]{ new { date = DateOnly.FromDateTime(DateTime.Today), temperatureC = 25, summary = "Warm" } };
                var json = JsonSerializer.Serialize(weather, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            }

            if (payload is not null)
            {
                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
