using System.Net.Http;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client;
using Mississippi.Reservoir.Client;
using Mississippi.Reservoir.Client.BuiltIn;

using Spring.Client;
using Spring.Client.AuthSimulation;
using Spring.Client.Features;
using Spring.Client.Features.AuthProofAggregate;
using Spring.Client.Features.AuthProofSaga;
using Spring.Client.Features.AuthSimulation;
using Spring.Client.Features.BankAccountAggregate;
using Spring.Client.Features.BankAccountBalance.Dtos;
using Spring.Client.Features.DemoAccounts;
using Spring.Client.Features.DualEntitySelection;
using Spring.Client.Features.MoneyTransferSaga;
using Spring.Client.Features.MoneyTransferSagaAggregate;


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
// Write side + projection feature registrations
builder.Services.AddProjectionsFeature();
builder.Services.AddAuthProofAggregateFeature();
builder.Services.AddBankAccountAggregateFeature();
builder.Services.AddMoneyTransferSagaAggregateFeature();
builder.Services.AddAuthProofSagaFeature();
builder.Services.AddMoneyTransferSagaFeature();

// Navigation/UI: entity selection
builder.Services.AddDualEntitySelectionFeature();
builder.Services.AddDemoAccountsFeature();
builder.Services.AddAuthSimulationFeature();

// Built-in Reservoir features: navigation, lifecycle
builder.Services.AddReservoirBlazorBuiltIns();

// DevTools integration: enable Redux DevTools in development
builder.Services.AddReservoirDevTools(options =>
{
    options.Enablement = ReservoirDevToolsEnablement.Always;
    options.Name = "Spring Sample";
    options.IsStrictStateRehydrationEnabled = true;
});

// Configure Inlet with SignalR effect for real-time projection updates
// ScanProjectionDtos automatically discovers [ProjectionPath] types and wires up fetching
builder.Services.AddInletClient();
builder.Services.AddInletBlazorSignalR(signalR => signalR
    .WithHubPath("/hubs/inlet")
    .ScanProjectionDtos(typeof(BankAccountBalanceProjectionDto).Assembly));
await builder.Build().RunAsync();