using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;


namespace Crescent.WebApp.Client;

/// <summary>
///     Entry point for the Crescent WebAssembly client application.
/// </summary>
internal static class Program
{
    /// <summary>
    ///     Application entry point.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    private static async Task Main(
        string[] args
    )
    {
        WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
        await builder.Build().RunAsync().ConfigureAwait(true);
    }
}
