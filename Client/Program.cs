using Blazored.Toast;
using Client;
using Client.Auth;
using Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = new Uri(new Uri(builder.HostEnvironment.BaseAddress), builder.Configuration["apiBaseUrlUrl"] ?? "https://localhost:5117/");

// Named clients (optional but handy)
builder.Services.AddHttpClient("auth", client => client.BaseAddress = apiBaseUrl);
builder.Services.AddHttpClient("api", client => client.BaseAddress = apiBaseUrl);

// ===== Auth & state =====
builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();

// Small, decoupled token store avoids circular DI
builder.Services.AddScoped<IAccessTokenStore, MemoryAccessTokenStore>();

// Auth state provider only reads from token store
builder.Services.AddScoped<AuthenticationStateProvider, MemoryTokenAuthStateProvider>();

// Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<MenuService>();

// Message handler to attach bearer automatically
builder.Services.AddTransient<AuthHeaderHandler>();

// HttpClient that auto-sends access token (use when calling protected endpoints)
builder.Services.AddHttpClient("authorized-api", client => client.BaseAddress = apiBaseUrl)
    .AddHttpMessageHandler<AuthHeaderHandler>();

await builder.Build().RunAsync();