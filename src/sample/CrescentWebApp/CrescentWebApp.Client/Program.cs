using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

[assembly: ExcludeFromCodeCoverage]

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
await builder.Build().RunAsync().ConfigureAwait(true);