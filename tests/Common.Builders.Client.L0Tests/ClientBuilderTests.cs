using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.Common.Builders.Client.Abstractions;


namespace Mississippi.Common.Builders.Client.L0Tests;

/// <summary>
///     L0 tests for <see cref="ClientBuilder" />.
/// </summary>
public sealed class ClientBuilderTests
{
    [Fact]
    public void ConfigureClientShouldConfigureOptions()
    {
        IClientBuilder builder = ClientBuilder.Create();
        builder.ConfigureClient(options => { options.RoutePrefix = "/api/projections"; });
        ServiceProvider provider = builder.Services.BuildServiceProvider();
        IOptions<ClientBuilderOptions> options = provider.GetRequiredService<IOptions<ClientBuilderOptions>>();
        Assert.Equal("/api/projections", options.Value.RoutePrefix);
    }

    [Fact]
    public void ConfigureClientShouldReturnSameBuilderInstance()
    {
        IClientBuilder builder = ClientBuilder.Create();
        IClientBuilder result = builder.ConfigureClient(_ => { });
        Assert.Same(builder, result);
    }

    [Fact]
    public void ConfigureClientShouldThrowWhenConfigureIsNull()
    {
        IClientBuilder builder = ClientBuilder.Create();
        Action<ClientBuilderOptions>? configure = null;
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => builder.ConfigureClient(configure!));
        Assert.Equal("configure", exception.ParamName);
    }

    [Fact]
    public void CreateShouldProvideServiceCollection()
    {
        IClientBuilder builder = ClientBuilder.Create();
        Assert.NotNull(builder.Services);
    }
}