using Microsoft.AspNetCore.Builder;

using Mississippi.Sdk.Silo;

using Spring.Silo;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Composite Spring silo registrations
builder.AddMississippiSilo()
	.AddSpringSilo();
WebApplication app = builder.Build();

// Composite Spring silo endpoint mapping
app.UseSpringSilo();
await app.RunAsync();