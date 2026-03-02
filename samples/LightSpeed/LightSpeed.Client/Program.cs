using LightSpeed.Client;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Builders.Client;
using Mississippi.Reservoir.Client;
using Mississippi.Reservoir.Core;


WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
ClientBuilder client = ClientBuilder.Create();
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
client.Services.AddReservoir();
client.Services.AddReservoirDevTools();
foreach (ServiceDescriptor descriptor in client.Services)
{
    builder.Services.Add(descriptor);
}

await builder.Build().RunAsync();