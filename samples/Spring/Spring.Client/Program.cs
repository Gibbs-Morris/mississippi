using System.Net.Http;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir;

using Spring.Client;
using Spring.Client.Features.BankAccountAggregate;
using Spring.Client.Features.BankAccountBalance;


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

// Read side: projection queries
builder.Services.AddBankAccountBalanceFeature();

// Register the Reservoir store (after all features)
builder.Services.AddReservoir();
await builder.Build().RunAsync();