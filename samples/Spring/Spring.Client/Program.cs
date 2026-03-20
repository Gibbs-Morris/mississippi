using System.Net.Http;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Client;
using Mississippi.Reservoir.Client.BuiltIn;
using Mississippi.Sdk.Client;

using MississippiSamples.Spring.Client;
using MississippiSamples.Spring.Client.AuthSimulation;
using MississippiSamples.Spring.Client.Features;
using MississippiSamples.Spring.Client.Features.AuthSimulation;
using MississippiSamples.Spring.Client.Features.DemoAccounts;
using MississippiSamples.Spring.Client.Features.DualEntitySelection;


WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient to call the API (base address is the host origin)
builder.Services.AddScoped<AuthSimulationHeadersHandler>();
#pragma warning disable IDISP014 // Blazor WASM DI manages HttpClient lifecycle
builder.Services.AddScoped(sp =>
{
    AuthSimulationHeadersHandler authSimulationHeadersHandler = sp.GetRequiredService<AuthSimulationHeadersHandler>();
    authSimulationHeadersHandler.InnerHandler = new HttpClientHandler();
    return new HttpClient(authSimulationHeadersHandler)
    {
        BaseAddress = new(builder.HostEnvironment.BaseAddress),
    };
});
#pragma warning restore IDISP014
builder.UseMississippi(client =>
{
    // Generated extension: registers all command-client mappers, projection DTO
    // reducers, saga-client state, AND Reservoir projection-path registrations
    // for the Spring domain in one call.
    client.AddMississippiSamplesSpringDomainClient();

    // Enable the Inlet real-time projection subscription client.
    client.AddInletClient();

    // Connect Inlet to the gateway's SignalR hub for live projection updates.
    client.AddInletBlazorSignalR(signalR => signalR.WithHubPath("/hubs/inlet"));

    // Custom app-specific client state (not generated).
    client.Reservoir(reservoir =>
    {
        // Navigation/UI: entity selection
        reservoir.AddDualEntitySelectionFeature();
        reservoir.AddDemoAccountsFeature();
        reservoir.AddAuthSimulationFeature();

        // Built-in Reservoir features: navigation, lifecycle
        reservoir.AddReservoirBlazorBuiltIns();

        // DevTools integration: enable Redux DevTools in development
        reservoir.AddReservoirDevTools(options =>
        {
            options.Enablement = ReservoirDevToolsEnablement.Always;
            options.Name = "Spring Sample";
            options.IsStrictStateRehydrationEnabled = true;
        });
    });
});
await builder.Build().RunAsync();