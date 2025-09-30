using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ai_mate_blazor;
using ai_mate_blazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API base (overridable via appsettings or query string later)
var apiBase = builder.Configuration["API_BASE"] ?? "http://localhost:5280";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBase) });
builder.Services.AddScoped<ApiClient>();
builder.Services.AddScoped<EncryptionService>();
builder.Services.AddScoped<HmrcValidationService>();
builder.Services.AddScoped<VoiceStorageService>();
builder.Services.AddScoped<VoiceService>();
builder.Services.AddScoped<VoiceSecurityService>();

await builder.Build().RunAsync();
