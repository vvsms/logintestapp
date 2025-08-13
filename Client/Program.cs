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

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:5117/";

// Register TokenProvider (in-memory)
builder.Services.AddSingleton<TokenProvider>(); // singleton ok for in-memory per-browser-tab

// Register auth services and interface (IAuthService)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AuthService>(); // concrete too if you need direct access

// AuthenticationStateProvider uses the interface IAuthService (breaks circular deps)
builder.Services.AddScoped<AuthenticationStateProvider, MemoryTokenAuthStateProvider>();

// Menu service (uses ApiClient)
builder.Services.AddScoped<MenuService>();

// AuthMessageHandler — attaches Bearer token to ApiClient requests and attempts refresh
builder.Services.AddScoped<AuthMessageHandler>();

// Named HttpClients:
// NoAuthClient - for login/refresh/logout (server reads HttpOnly cookie)
builder.Services.AddHttpClient("NoAuthClient", client => client.BaseAddress = new Uri(apiBaseUrl));

// ApiClient - for all protected API calls, uses AuthMessageHandler
builder.Services.AddHttpClient("ApiClient", client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<AuthMessageHandler>();

builder.Services.AddAuthorizationCore();
builder.Services.AddBlazoredToast();

// Build host so we can do silent refresh before rendering UI
var host = builder.Build();

// Attempt silent refresh on startup to populate in-memory access token (if refresh cookie exists)
try
{
    var auth = host.Services.GetRequiredService<IAuthService>();
    await (auth as AuthService)!.TrySilentRefreshAsync(); // call concrete method to populate token
}
catch
{
    // failing silent refresh is okay — user will be anonymous
}

await host.RunAsync();