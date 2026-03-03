using System.Net.Http;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Builders.Client;
using Mississippi.Inlet.Client;
using Mississippi.Reservoir.Builder;
using Mississippi.Reservoir.Client;

using Spring.Client;
using Spring.Client.AuthSimulation;
using Spring.Client.Features;
using Spring.Client.Features.AuthSimulation;
using Spring.Client.Features.BankAccountBalance.Dtos;
using Spring.Client.Features.DemoAccounts;
using Spring.Client.Features.DualEntitySelection;


WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
ClientBuilder client = ClientBuilder.Create();

// Configure HttpClient to call the API (base address is the host origin)
client.Services.AddScoped<AuthSimulationHeadersHandler>();
#pragma warning disable IDISP014 // Blazor WASM DI manages HttpClient lifecycle
client.Services.AddScoped(sp =>
{
    AuthSimulationHeadersHandler authSimulationHeadersHandler = sp.GetRequiredService<AuthSimulationHeadersHandler>();
    authSimulationHeadersHandler.InnerHandler = new HttpClientHandler();
    return new HttpClient(authSimulationHeadersHandler)
    {
        BaseAddress = new(builder.HostEnvironment.BaseAddress),
    };
});
#pragma warning restore IDISP014

// Write side + projection feature registrations
client.AddSpringDomain();

// Reservoir state management: features, built-ins, and DevTools
client.AddReservoir(reservoir =>
{
    // Navigation/UI features
    reservoir.AddDualEntitySelectionFeature();
    reservoir.AddDemoAccountsFeature();
    reservoir.AddAuthSimulationFeature();

    // Built-in Reservoir features: navigation, lifecycle
    reservoir.AddBuiltIns();

    // DevTools integration: enable Redux DevTools in development
    reservoir.AddDevTools(options =>
    {
        options.Enablement = ReservoirDevToolsEnablement.Always;
        options.Name = "Spring Sample";
        options.IsStrictStateRehydrationEnabled = true;
    });
});

// Configure Inlet with SignalR effect for real-time projection updates
// ScanProjectionDtos automatically discovers [ProjectionPath] types and wires up fetching
client.AddInlet();
client.AddInletSignalR(signalR => signalR
    .WithHubPath("/hubs/inlet")
    .ScanProjectionDtos(typeof(BankAccountBalanceProjectionDto).Assembly));
builder.UseMississippi(client);
await builder.Build().RunAsync();