using System.Net.Http;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet;
using Mississippi.Inlet.Blazor.WebAssembly;

using Spring.Client;
using Spring.Client.Features.BankAccountAggregate;
using Spring.Client.Features.BankAccountBalance.Dtos;


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

// Register features (one line per feature - scales cleanly)
// Write side: aggregate commands
builder.Services.AddBankAccountAggregateFeature();

// Configure Inlet with SignalR effect for real-time projection updates
// ScanProjectionDtos automatically discovers [ProjectionPath] types and wires up fetching
builder.Services.AddInlet();
builder.Services.AddInletBlazorSignalR(signalR => signalR
    .WithHubPath("/hubs/inlet")
    .ScanProjectionDtos(typeof(BankAccountBalanceProjectionDto).Assembly));
await builder.Build().RunAsync();