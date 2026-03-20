using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using Mississippi.Reservoir.Client;
using Mississippi.Reservoir.Client.BuiltIn.Lifecycle;

using MississippiSamples.LightSpeed.Client;


WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.AddReservoir(reservoir => reservoir.AddBuiltInLifecycle().AddReservoirDevTools());
await builder.Build().RunAsync();