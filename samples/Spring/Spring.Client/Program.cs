using System.Net.Http;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Builders;
using Mississippi.Inlet.Client;
using Mississippi.Reservoir.Abstractions.Builders;
using Mississippi.Reservoir.Blazor;
using Mississippi.Reservoir.Blazor.BuiltIn;
using Mississippi.Sdk.Client;

using Spring.Client;
using Spring.Client.Features.BankAccountBalance.Dtos;
using Spring.Client.Features.DemoAccounts;
using Spring.Client.Features.DualEntitySelection;
using Spring.Client.Registrations;


WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
IMississippiClientBuilder mississippi = builder.AddMississippiClient();

// Configure HttpClient to call the API (base address is the host origin)
#pragma warning disable IDISP014 // Blazor WASM DI manages HttpClient lifecycle
builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new(builder.HostEnvironment.BaseAddress),
});
#pragma warning restore IDISP014

// Register features (one line per feature - scales cleanly)
// Write side: aggregate commands
IReservoirBuilder reservoir = mississippi.AddInletClient();
reservoir.AddSpringDomain();

// Navigation/UI: entity selection
reservoir.AddDualEntitySelectionFeature();
reservoir.AddDemoAccountsFeature();

// Built-in Reservoir features: navigation, lifecycle
reservoir.AddReservoirBlazorBuiltIns();

// DevTools integration: enable Redux DevTools in development
reservoir.AddReservoirDevTools(options =>
{
    options.Enablement = ReservoirDevToolsEnablement.Always;
    options.Name = "Spring Sample";
    options.IsStrictStateRehydrationEnabled = true;
});

// Configure Inlet with SignalR effect for real-time projection updates
// ScanProjectionDtos automatically discovers [ProjectionPath] types and wires up fetching
reservoir.AddInletBlazorSignalR(signalR => signalR
    .WithHubPath("/hubs/inlet")
    .ScanProjectionDtos(typeof(BankAccountBalanceProjectionDto).Assembly));
await builder.Build().RunAsync();