using System.Diagnostics.CodeAnalysis;

using Mississippi.CrescentWebApp.Components;

using _Imports = Mississippi.CrescentWebApp.Client._Imports;


#pragma warning disable SA1516 // ElementsMustBeSeparatedByBlankLine
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", true);

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveWebAssemblyRenderMode().AddAdditionalAssemblies(typeof(_Imports).Assembly);
await app.RunAsync().ConfigureAwait(true);

/// <summary>
/// Marker type used by ASP.NET Core to locate the assembly containing Blazor components.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "This is a marker type, no implementation needed.")]
internal static partial class Program
{
}