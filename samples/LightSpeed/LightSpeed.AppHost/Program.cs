using Aspire.Hosting;

using Projects;


IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
builder.AddProject<LightSpeed_Gateway>("lightspeed-gateway").WithExternalHttpEndpoints();
await builder.Build().RunAsync();