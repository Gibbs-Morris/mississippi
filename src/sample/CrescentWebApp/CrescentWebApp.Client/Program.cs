using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;


namespace Mississippi.CrescentWebApp.Client;

/// <summary>
///     Marker type used by ASP.NET Core to locate the assembly containing Blazor components.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "This is a marker type, no implementation needed.")]
internal static class Program
{
    public static async Task Main(
        string[] args
    )
    {
        WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
        await builder.Build().RunAsync().ConfigureAwait(true);
    }
}