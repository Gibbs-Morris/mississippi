using System;
using System.Net.Http;

using Cascade.Web.Client;
using Cascade.Web.Client.Services;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;


WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient to call the API (base address is the host origin)
// Blazor WASM scoped lifetime acts as a singleton; this is the official Microsoft pattern.
// See: https://learn.microsoft.com/aspnet/core/blazor/call-web-api#add-the-httpclient-service
#pragma warning disable IDISP014 // Blazor WASM DI manages HttpClient lifecycle
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
#pragma warning restore IDISP014

// Add SignalR message service
builder.Services.AddScoped<IMessageService, SignalRMessageService>();

await builder.Build().RunAsync();
