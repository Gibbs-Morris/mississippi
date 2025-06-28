using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;


WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
await builder.Build().RunAsync().ConfigureAwait(true);

/// <summary>
/// Marker type used by ASP.NET Core to locate the assembly containing Blazor components.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "This is a marker type, no implementation needed.")]
internal static partial class Program
{
}