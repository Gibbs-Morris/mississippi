using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using Mississippi.Refraction.Client.Infrastructure;
using Mississippi.Refraction.Client.StateManagement.Infrastructure;
using Mississippi.Reservoir.Core;

using MississippiSamples.LightSpeed.Client;
using MississippiSamples.LightSpeed.Client.Features.ReservoirOperationsWorkbench;


WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddRefraction();
builder.Services.AddRefractionReservoirPages();
builder.Services.AddReservoir().AddReservoirOperationsWorkbenchFeature();
await builder.Build().RunAsync();