using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;


#pragma warning disable SA1516 // ElementsMustBeSeparatedByBlankLine
[assembly: ExcludeFromCodeCoverage]
WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
await builder.Build().RunAsync().ConfigureAwait(true);