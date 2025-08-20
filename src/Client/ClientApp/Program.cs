using ClientApp;
using ClientApp.Services;
using ClientApp.State;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBase = builder.Configuration["ApiBase"] ?? "http://localhost:5096";

// Token state + auth state provider
builder.Services.AddSingleton<TokenState>();
builder.Services.AddScoped<AuthStateProvider>();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AuthStateProvider>());

// Handlers
builder.Services.AddTransient<AuthMessageHandler>();

// Raw client (sends cookies, no bearer)
builder.Services.AddHttpClient("ApiRaw", client =>
{
    client.BaseAddress = new Uri(apiBase);
});

// Authenticated client (Bearer + cookies)
builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri(apiBase);
}).AddHttpMessageHandler<AuthMessageHandler>();

// High-level services
builder.Services.AddScoped<AuthService>();

await builder.Build().RunAsync();
