using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using CampusEats.Frontend;
using CampusEats.Frontend.Services;
using CampusEats.Frontend.State;
using CampusEats.Frontend.Features.Auth;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Schimbă la 8080 dacă API-ul tău rulează din Docker compose pe 8080
var apiBase = Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:5156";

builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<CampusEats.Frontend.Services.OrderService>();
builder.Services.AddAuthorizationCore();

builder.Services.AddTransient<AuthHeaderHandler>();
builder.Services.AddTransient<DebugAuthHandler>();

// Client implicit “authorized” (orice @inject HttpClient va avea Bearer)
builder.Services.AddHttpClient("authorized", client =>
{
    client.BaseAddress = new Uri(apiBase);
})
.AddHttpMessageHandler<AuthHeaderHandler>()
.AddHttpMessageHandler<DebugAuthHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("authorized"));

// ApiClient tipizat – tot cu Bearer
builder.Services.AddHttpClient<ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBase);
})
.AddHttpMessageHandler<AuthHeaderHandler>()
.AddHttpMessageHandler<DebugAuthHandler>();

builder.Services.AddScoped<CartService>();

var host = builder.Build();

// Rehidratează token-ul înainte de primul request
var auth = host.Services.GetRequiredService<AuthService>();

try 
{
    // Încercăm să rehidratăm utilizatorul, dar nu lăsăm o eroare să oprească tot site-ul
    await auth.InitializeAsync();
}
catch (Exception ex)
{
    // Doar scriem eroarea în consolă, dar lăsăm aplicația să continue
    Console.WriteLine($"[CRITIC] Eroare la inițializarea Auth: {ex.Message}");
}

await host.RunAsync();