using System.Net.Http;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Sdk.Client;

using Spring.Client;
using Spring.Client.Features.EntitySelection;


WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient to call the API (base address is the host origin)
#pragma warning disable IDISP014 // Blazor WASM DI manages HttpClient lifecycle
builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new(builder.HostEnvironment.BaseAddress),
});
#pragma warning restore IDISP014

// Register all Inlet features (aggregate commands, Reservoir built-ins, SignalR)
// Generated from [assembly: GenerateInletClientComposite(AppName = "Spring")]
builder.AddMississippiClient()
    .AddSpringInlet();

// Application-specific features (not covered by composite generator)
builder.Services.AddEntitySelectionFeature();
await builder.Build().RunAsync();