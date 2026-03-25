using System.Net.Http;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Hosting.Client;
using Mississippi.Inlet.Client;
using Mississippi.Reservoir.Client;
using Mississippi.Reservoir.Client.BuiltIn;

using MississippiSamples.Spring.Client;
using MississippiSamples.Spring.Client.AuthSimulation;
using MississippiSamples.Spring.Client.Features;
using MississippiSamples.Spring.Client.Features.AuthSimulation;
using MississippiSamples.Spring.Client.Features.BankAccountBalance.Dtos;
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
builder.AddMississippiClient(client =>
{
    client.AddMississippiSamplesSpringDomainClient()
        .Reservoir(reservoir =>
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

            // Configure Inlet with SignalR effect for real-time projection updates
            // ScanProjectionDtos automatically discovers [ProjectionPath] types and wires up fetching
            reservoir.AddInletBlazorSignalR(signalR => signalR
                .WithHubPath("/hubs/inlet")
                .ScanProjectionDtos(typeof(BankAccountBalanceProjectionDto).Assembly));
        });
});
await builder.Build().RunAsync();