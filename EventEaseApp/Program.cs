using EventEaseApp;
using EventEaseApp.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Scoped state provides the shared catalog and participation state across routed pages.
// SessionState persists only event IDs/check-in flags in sessionStorage, never PII.
builder.Services.AddScoped<EventCatalog>();
builder.Services.AddScoped<SessionState>();

await builder.Build().RunAsync();
