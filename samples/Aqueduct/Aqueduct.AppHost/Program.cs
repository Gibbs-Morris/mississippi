using Aspire.Hosting;

// Aqueduct AppHost - Minimal Aspire host for Aqueduct L2 testing.
// Unlike Crescent, Aqueduct does not require Cosmos DB or Blob storage
// because SignalR grains are ephemeral (no persistent state).
// This host exists primarily to enable Aspire.Hosting.Testing patterns
// and future expansion (e.g., Redis backplane testing).
IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Currently no external resources needed - Orleans uses in-memory streams.
// Future: Add Redis for backplane testing if needed.
await builder.Build().RunAsync();
