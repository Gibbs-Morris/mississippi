using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


namespace Mississippi.Sdk.Server.L0Tests;

/// <summary>
///     Tests for <see cref="MississippiServerRegistrations" />.
/// </summary>
public sealed class MississippiServerRegistrationsTests
{
    /// <summary>
    ///     AddMississippiServer should register options and apply configuration.
    /// </summary>
    [Fact]
    public void AddMississippiServerRegistersOptionsAndAppliesConfiguration()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddMississippiServer(options =>
        {
            options.EnableCors = false;
            options.ApiPrefix = "/api/v2";
            options.HubPathPrefix = "/hubs-custom";
        });
        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        IOptions<MississippiServerOptions> options = provider.GetRequiredService<IOptions<MississippiServerOptions>>();
        Assert.False(options.Value.EnableCors);
        Assert.Equal("/api/v2", options.Value.ApiPrefix);
        Assert.Equal("/hubs-custom", options.Value.HubPathPrefix);
    }
}