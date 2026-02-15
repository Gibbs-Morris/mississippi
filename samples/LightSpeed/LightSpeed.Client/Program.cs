using LightSpeed.Client;
using LightSpeed.Client.Features.KitchenSinkFeatures;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using Mississippi.Reservoir;
using Mississippi.Reservoir.Blazor;


WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddReservoir();
builder.Services.AddKitchenSinkFeature();
builder.Services.AddReservoirDevTools(options =>
{
    options.Enablement = ReservoirDevToolsEnablement.Always;
    options.Name = "LightSpeed Sample";
    options.IsStrictStateRehydrationEnabled = true;
});
await builder.Build().RunAsync();