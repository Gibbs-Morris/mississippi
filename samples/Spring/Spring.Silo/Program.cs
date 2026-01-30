using Microsoft.AspNetCore.Builder;

using Spring.Silo.Infrastructure;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Composite Spring silo registrations
builder.AddSpringSilo();
WebApplication app = builder.Build();

// Composite Spring silo endpoint mapping
app.UseSpringSilo();
await app.RunAsync();