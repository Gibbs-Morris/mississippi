using Microsoft.AspNetCore.Builder;

using Mississippi.Sdk.Server;

using Spring.Server;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Composite Spring server registrations
builder.AddMississippiServer().AddSpringServer();
WebApplication app = builder.Build();

// Composite Spring middleware + endpoint mapping
app.UseSpringServer();
await app.RunAsync();