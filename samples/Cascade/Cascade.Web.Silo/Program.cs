using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add Aspire-managed Azure Storage clients for Orleans clustering and grain state
// These are configured by the AppHost via WithReference
// Must use Keyed variants so Orleans can resolve via GetRequiredKeyedService<T>(serviceKey)
builder.AddKeyedAzureTableServiceClient("clustering");
builder.AddKeyedAzureBlobServiceClient("grainstate");

// Configure Orleans silo with Aspire integration
// UseOrleans() automatically configures clustering, grain storage, and streaming
// based on the AppHost configuration (WithClustering, WithGrainStorage, WithMemoryStreaming)
builder.UseOrleans();

WebApplication app = builder.Build();

// Health check endpoint for Aspire orchestration
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Cascade.Web.Silo" }));

await app.RunAsync();
