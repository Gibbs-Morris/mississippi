using LightSpeed.Client;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using Mississippi.Reservoir;
using Mississippi.Reservoir.Blazor;
using Mississippi.Reservoir.Blazor.BuiltIn;


WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddReservoir();
builder.Services.AddReservoirBlazorBuiltIns();
builder.Services.AddReservoirDevTools(options =>
{
    options.Enablement = ReservoirDevToolsEnablement.Always;
    options.Name = "Lightspeed Sample";
    options.IsStrictStateRehydrationEnabled = true;
});
await builder.Build().RunAsync();