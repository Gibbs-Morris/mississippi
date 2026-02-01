using Aspire.Hosting;

using Projects;


IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);
builder.AddProject<LightSpeed_Server>("lightspeed-server").WithExternalHttpEndpoints();
await builder.Build().RunAsync();