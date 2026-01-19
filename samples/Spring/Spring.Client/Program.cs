using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Spring.Client;


WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient to call the API (base address is the host origin)
#pragma warning disable IDISP014 // Blazor WASM DI manages HttpClient lifecycle
builder.Services.AddScoped(_ => new System.Net.Http.HttpClient
{
    BaseAddress = new System.Uri(builder.HostEnvironment.BaseAddress),
});
#pragma warning restore IDISP014
await builder.Build().RunAsync();
