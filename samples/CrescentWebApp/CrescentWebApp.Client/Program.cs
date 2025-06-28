using Microsoft.AspNetCore.Components.WebAssembly.Hosting;


namespace Mississippi.CrescentWebApp.Client;

internal static class Program
{
    private static async Task Main(
        string[] args
    )
    {
        WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
        await builder.Build().RunAsync().ConfigureAwait(true);
    }
}