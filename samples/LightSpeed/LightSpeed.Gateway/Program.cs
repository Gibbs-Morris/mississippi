using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
WebApplication app = builder.Build();
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();
app.MapGet(
    "/health",
    () => Results.Ok(
        new
        {
            status = "healthy",
        }));
app.MapFallbackToFile("index.html");
await app.RunAsync();