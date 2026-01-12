using System.Net.Http;

using Cascade.Client;
using Cascade.Client.Cart;
using Cascade.Client.Services;
using Cascade.Contracts;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet;
using Mississippi.Inlet.Blazor.WebAssembly;
using Mississippi.Reservoir;


WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient to call the API (base address is the host origin)
// In Blazor WASM, scoped lifetime effectively acts as singleton at runtime.
// Using scoped follows the Fluxor pattern and is correct for Blazor Server too.
// See: https://learn.microsoft.com/aspnet/core/blazor/call-web-api#add-the-httpclient-service
#pragma warning disable IDISP014 // Blazor WASM DI manages HttpClient lifecycle
builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new(builder.HostEnvironment.BaseAddress),
});
#pragma warning restore IDISP014

// Add SignalR message service (scoped to match Fluxor pattern)
builder.Services.AddScoped<IMessageService, SignalRMessageService>();

// Configure Inlet with SignalR effect for real-time projection updates
// ScanProjectionDtos automatically discovers [UxProjectionDto] types and wires up fetching
// Configure store with feature states via the configureStore callback
builder.Services.AddInlet(store => store.RegisterState<CartState>());
builder.Services.AddInletBlazorSignalR(signalR => signalR
    .WithHubPath("/hubs/inlet")
    .ScanProjectionDtos(typeof(ConversationMessagesResponse).Assembly));

// Configure Reservoir (Redux-style state management)
// Register reducers for cart actions
builder.Services.AddReducer<AddItemAction, CartState>(CartReducers.AddItem);
builder.Services.AddReducer<RemoveItemAction, CartState>(CartReducers.RemoveItem);
builder.Services.AddReducer<ProductsLoadingAction, CartState>(CartReducers.ProductsLoading);
builder.Services.AddReducer<ProductsLoadedAction, CartState>(CartReducers.ProductsLoaded);
builder.Services.AddReducer<ProductsLoadFailedAction, CartState>(CartReducers.ProductsLoadFailed);

// Register effects for async operations
builder.Services.AddEffect<LoadProductsEffect>();

// AddReservoir registers remaining Reservoir services (reducers are already registered above)
builder.Services.AddReservoir();
await builder.Build().RunAsync();