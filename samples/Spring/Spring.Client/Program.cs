using System.Net.Http;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Client;
using Mississippi.Reservoir.Client.BuiltIn;

using MississippiSamples.Spring.Client;
using MississippiSamples.Spring.Client.AuthSimulation;
using MississippiSamples.Spring.Client.Features;
using MississippiSamples.Spring.Client.Features.AuthProofAggregate;
using MississippiSamples.Spring.Client.Features.AuthProofSaga;
using MississippiSamples.Spring.Client.Features.AuthSimulation;
using MississippiSamples.Spring.Client.Features.BankAccountAggregate;
using MississippiSamples.Spring.Client.Features.BankAccountBalance.Dtos;
using MississippiSamples.Spring.Client.Features.DemoAccounts;
using MississippiSamples.Spring.Client.Features.DualEntitySelection;
using MississippiSamples.Spring.Client.Features.MoneyTransferSaga;
using MississippiSamples.Spring.Client.Features.MoneyTransferSagaAggregate;


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

// Register features (one line per feature - scales cleanly)
IReservoirBuilder reservoir = builder.AddReservoir();

// Register features (one line per feature - scales cleanly)
// Write side + projection feature registrations
reservoir.AddProjectionsFeature();
reservoir.AddAuthProofAggregateFeature();
reservoir.AddBankAccountAggregateFeature();
reservoir.AddMoneyTransferSagaAggregateFeature();
reservoir.AddAuthProofSagaFeature();
reservoir.AddMoneyTransferSagaFeature();

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
reservoir.AddInletClient();
reservoir.AddInletBlazorSignalR(signalR => signalR
    .WithHubPath("/hubs/inlet")
    .ScanProjectionDtos(typeof(BankAccountBalanceProjectionDto).Assembly));
await builder.Build().RunAsync();