using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.Common.Builders.Abstractions;
using Mississippi.Common.Builders.Client.Abstractions;


namespace Mississippi.Common.Builders.Client.L0Tests;

/// <summary>
///     L0 tests for <see cref="ClientBuilder" />.
/// </summary>
public sealed class ClientBuilderTests
{
    /// <summary>
    ///     ConfigureClient applies the provided options action.
    /// </summary>
    [Fact]
    public void ConfigureClientShouldConfigureOptions()
    {
        ClientBuilder builder = ClientBuilder.Create();
        builder.ConfigureClient(options => { options.RoutePrefix = "/api/projections"; });
        using ServiceProvider provider = builder.Services.BuildServiceProvider();
        IOptions<ClientBuilderOptions> options = provider.GetRequiredService<IOptions<ClientBuilderOptions>>();
        Assert.Equal("/api/projections", options.Value.RoutePrefix);
    }

    /// <summary>
    ///     ConfigureClient returns the same builder instance.
    /// </summary>
    [Fact]
    public void ConfigureClientShouldReturnSameBuilderInstance()
    {
        ClientBuilder builder = ClientBuilder.Create();
        IClientBuilder result = builder.ConfigureClient(_ => { });
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     ConfigureClient throws when configure is null.
    /// </summary>
    [Fact]
    public void ConfigureClientShouldThrowWhenConfigureIsNull()
    {
        ClientBuilder builder = ClientBuilder.Create();
        Action<ClientBuilderOptions>? configure = null;
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => builder.ConfigureClient(configure!));
        Assert.Equal("configure", exception.ParamName);
    }

    /// <summary>
    ///     Create initializes a service collection.
    /// </summary>
    [Fact]
    public void CreateShouldProvideServiceCollection()
    {
        ClientBuilder builder = ClientBuilder.Create();
        Assert.NotNull(builder.Services);
    }

    /// <summary>
    ///     Validate returns no diagnostics for client builders.
    /// </summary>
    [Fact]
    public void ValidateShouldReturnNoDiagnostics()
    {
        ClientBuilder builder = ClientBuilder.Create();
        IReadOnlyList<BuilderDiagnostic> diagnostics = builder.Validate();
        Assert.Empty(diagnostics);
    }
}