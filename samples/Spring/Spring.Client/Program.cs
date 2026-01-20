using System.Net.Http;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir;

using Spring.Client;
using Spring.Client.Features.Greet.Actions;
using Spring.Client.Features.Greet.Effects;
using Spring.Client.Features.Greet.Reducers;
using Spring.Client.Features.Greet.State;


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

// Register Reservoir reducers for the greet feature
builder.Services.AddReducer<GreetLoadingAction, GreetState>(GreetReducers.Loading);
builder.Services.AddReducer<GreetSucceededAction, GreetState>(GreetReducers.Succeeded);
builder.Services.AddReducer<GreetFailedAction, GreetState>(GreetReducers.Failed);

// Register effects
builder.Services.AddEffect<GreetEffect>();

// Register the Reservoir store (after all reducers and effects)
builder.Services.AddReservoir();
await builder.Build().RunAsync();