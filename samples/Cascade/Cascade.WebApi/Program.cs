using System.Threading.Tasks;

using Cascade.Components.Services;
using Cascade.Domain;
using Cascade.Domain.Projections.ChannelMemberList;
using Cascade.Domain.Projections.ChannelMessages;
using Cascade.Domain.Projections.UserProfile;
using Cascade.WebApi.Components;
using Cascade.WebApi.Services;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Mississippi.EventSourcing.Brooks;
using Mississippi.EventSourcing.Brooks.Cosmos;
using Mississippi.EventSourcing.Serialization.Json;
using Mississippi.EventSourcing.Snapshots.Cosmos;
using Mississippi.EventSourcing.UxProjections.SignalR;
using Mississippi.Ripples.Blazor;
using Mississippi.Ripples.Server;


namespace Cascade.WebApi;

/// <summary>
///     Entry point for the Cascade WebApi host that serves the Blazor WASM client.
/// </summary>
internal static class Program
{
    /// <summary>
    ///     Application entry point.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>A <see cref="Task" /> representing the asynchronous operation.</returns>
    public static async Task Main(
        string[] args
    )
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Add Aspire-managed clients - these register CosmosClient and BlobServiceClient
        builder.AddAzureCosmosClient("cosmos");
        builder.AddAzureBlobServiceClient("blobs");
        builder.AddAzureTableServiceClient("clustering");

        // Add Blazor services with Interactive WebAssembly rendering
        builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();

        // Add SignalR for real-time projections
        builder.Services.AddSignalR();
        builder.Services.AddUxProjectionSignalR();

        // Add domain services (aggregates, handlers, reducers, projections)
        builder.Services.AddCascadeDomain();

        // Add HttpContextAccessor for user context
        builder.Services.AddHttpContextAccessor();

        // Add user context for server-side operations
        builder.Services.AddScoped<IUserContext, ServerUserContext>();
        builder.Services.AddScoped<IChatService, ServerChatService>();

        // Add Ripples Blazor infrastructure (IProjectionCache)
        builder.Services.AddRipplesBlazor();

        // Add Ripples Server infrastructure (IProjectionUpdateNotifier)
        builder.Services.AddRipplesServer();

        // Register server ripples for each projection type
        builder.Services.AddServerRipple<UserProfileProjection>();
        builder.Services.AddServerRipple<ChannelMemberListProjection>();
        builder.Services.AddServerRipple<ChannelMessagesProjection>();

        // Add event sourcing services to DI container
        builder.Services.AddJsonSerialization();
        builder.Services.AddEventSourcingByService();

        // Configure Orleans client - Aspire handles clustering configuration
        builder.UseOrleansClient();

        // Add Cosmos storage providers for event sourcing
        builder.Services.AddCosmosBrookStorageProvider(options =>
        {
            options.DatabaseId = "cascade-db";
        });

        builder.Services.AddCosmosSnapshotStorageProvider(options =>
        {
            options.DatabaseId = "cascade-db";
            options.ContainerId = "snapshots";
        });

        // Add API controllers for chat operations
        builder.Services.AddControllers();

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAntiforgery();

        // Map API controllers
        app.MapControllers();

        // Map SignalR hub for real-time UX projections
        app.MapUxProjectionHub();

        // Map Blazor components with WASM interactivity
        app.MapRazorComponents<App>()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(Cascade.WebApi.Client._Imports).Assembly);

        await app.RunAsync();
    }
}
