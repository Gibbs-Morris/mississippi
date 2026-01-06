using System;
using System.Net.Http;
using System.Threading.Tasks;

using Cascade.Components.Services;
using Cascade.WebApi.Client.Services;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Ripples.Abstractions;


namespace Cascade.WebApi.Client;

/// <summary>
///     Entry point for the Cascade Blazor WebAssembly client.
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

        // Configure HttpClient using the standard Blazor WASM pattern
        // WebAssemblyHostBuilder provides built-in HttpClient factory support
        builder.Services.AddHttpClient(
            string.Empty,
            client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress));
        builder.Services.AddScoped(
            sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient());

        // Add user context for WASM (backed by local storage)
        builder.Services.AddScoped<IUserContext, WasmUserContext>();

        // Add chat service that calls HTTP API
        builder.Services.AddScoped<IChatService, HttpChatService>();

        // Register the WASM-compatible projection cache (creates its own SignalR connection)
        builder.Services.AddScoped<IProjectionCache, WasmProjectionCache>();

        await builder.Build().RunAsync();
    }
}
