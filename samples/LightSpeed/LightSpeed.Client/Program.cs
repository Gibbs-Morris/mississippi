#pragma warning disable CS0618 // Sample still demonstrates legacy composition pending issue #237.
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using Mississippi.Reservoir.Client;
using Mississippi.Reservoir.Core;

using MississippiSamples.LightSpeed.Client;


WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddReservoir();
builder.Services.AddReservoirDevTools();
await builder.Build().RunAsync();
#pragma warning restore CS0618