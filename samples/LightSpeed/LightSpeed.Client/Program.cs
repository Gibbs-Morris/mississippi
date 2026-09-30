using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using Mississippi.Reservoir.Client;

using MississippiSamples.LightSpeed.Client;
using MississippiSamples.LightSpeed.Client.Features.Showcase;


WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.AddReservoir().AddReservoirDevTools().AddShowcaseFeature();
await builder.Build().RunAsync();