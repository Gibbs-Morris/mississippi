using LightSpeed.Client;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using Mississippi.Common.Builders.Client;
using Mississippi.Reservoir.Builder;
using Mississippi.Reservoir.Client;


WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
ClientBuilder client = ClientBuilder.Create();
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
client.AddReservoir();
client.AddDevTools();
builder.UseMississippi(client);
await builder.Build().RunAsync();