using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;
using Mississippi.Common.Abstractions;

using NSubstitute;

using Orleans.Hosting;


namespace Mississippi.Aqueduct.Grains.L0Tests;

/// <summary>
///     Tests for <see cref="AqueductGrainsRegistrations" />.
/// </summary>
public sealed class AqueductGrainsRegistrationsTests
{
    /// <summary>
    ///     UseAqueduct should configure all options from AqueductSiloOptions.
    /// </summary>
    [Fact]
    public void UseAqueductConfiguresAllOptions()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = Substitute.For<ISiloBuilder>();
        siloBuilder.Services.Returns(services);

        // Act
        siloBuilder.UseAqueduct(options =>
        {
            options.StreamProviderName = "CustomStreams";
            options.ServerStreamNamespace = "CustomServer";
            options.AllClientsStreamNamespace = "CustomAllClients";
            options.HeartbeatIntervalMinutes = 5;
            options.DeadServerTimeoutMultiplier = 10;
        });

        // Assert
        using ServiceProvider provider = services.BuildServiceProvider();
        IOptions<AqueductOptions> options = provider.GetRequiredService<IOptions<AqueductOptions>>();
        Assert.Equal("CustomStreams", options.Value.StreamProviderName);
        Assert.Equal("CustomServer", options.Value.ServerStreamNamespace);
        Assert.Equal("CustomAllClients", options.Value.AllClientsStreamNamespace);
        Assert.Equal(5, options.Value.HeartbeatIntervalMinutes);
        Assert.Equal(10, options.Value.DeadServerTimeoutMultiplier);
    }

    /// <summary>
    ///     UseAqueduct should register AqueductGrainFactory as singleton.
    /// </summary>
    [Fact]
    public void UseAqueductRegistersAqueductGrainFactory()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = Substitute.For<ISiloBuilder>();
        siloBuilder.Services.Returns(services);

        // Act
        siloBuilder.UseAqueduct(_ => { });

        // Assert
        ServiceDescriptor? descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IAqueductGrainFactory));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    /// <summary>
    ///     UseAqueduct should register AqueductOptions.
    /// </summary>
    [Fact]
    public void UseAqueductRegistersAqueductOptions()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = Substitute.For<ISiloBuilder>();
        siloBuilder.Services.Returns(services);

        // Act
        siloBuilder.UseAqueduct(options => { options.StreamProviderName = "TestStreams"; });

        // Assert
        using ServiceProvider provider = services.BuildServiceProvider();
        IOptions<AqueductOptions> options = provider.GetRequiredService<IOptions<AqueductOptions>>();
        Assert.Equal("TestStreams", options.Value.StreamProviderName);
    }

    /// <summary>
    ///     UseAqueduct should return the same silo builder for chaining.
    /// </summary>
    [Fact]
    public void UseAqueductReturnsSameSiloBuilder()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = Substitute.For<ISiloBuilder>();
        siloBuilder.Services.Returns(services);

        // Act
        ISiloBuilder result = siloBuilder.UseAqueduct(_ => { });

        // Assert
        Assert.Same(siloBuilder, result);
    }

    /// <summary>
    ///     UseAqueduct should throw when configureOptions is null.
    /// </summary>
    [Fact]
    public void UseAqueductThrowsWhenConfigureOptionsIsNull()
    {
        // Arrange
        ISiloBuilder siloBuilder = Substitute.For<ISiloBuilder>();
        siloBuilder.Services.Returns(new ServiceCollection());

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => siloBuilder.UseAqueduct(null!));
        Assert.Equal("configureOptions", exception.ParamName);
    }

    /// <summary>
    ///     UseAqueduct should throw when siloBuilder is null.
    /// </summary>
    [Fact]
    public void UseAqueductThrowsWhenSiloBuilderIsNull()
    {
        // Arrange
        ISiloBuilder siloBuilder = null!;

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => siloBuilder.UseAqueduct(_ => { }));
        Assert.Equal("siloBuilder", exception.ParamName);
    }

    /// <summary>
    ///     UseAqueduct with default options should use MississippiDefaults.
    /// </summary>
    [Fact]
    public void UseAqueductWithDefaultOptionsUsesMississippiDefaults()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = Substitute.For<ISiloBuilder>();
        siloBuilder.Services.Returns(services);

        // Act
        siloBuilder.UseAqueduct();

        // Assert
        using ServiceProvider provider = services.BuildServiceProvider();
        IOptions<AqueductOptions> options = provider.GetRequiredService<IOptions<AqueductOptions>>();
        Assert.Equal(MississippiDefaults.StreamProviderName, options.Value.StreamProviderName);
        Assert.Equal(MississippiDefaults.StreamNamespaces.Server, options.Value.ServerStreamNamespace);
        Assert.Equal(MississippiDefaults.StreamNamespaces.AllClients, options.Value.AllClientsStreamNamespace);
        Assert.Equal(1, options.Value.HeartbeatIntervalMinutes);
        Assert.Equal(3, options.Value.DeadServerTimeoutMultiplier);
    }

    /// <summary>
    ///     UseAqueduct with no action should return the same silo builder.
    /// </summary>
    [Fact]
    public void UseAqueductWithNoActionReturnsSameSiloBuilder()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = Substitute.For<ISiloBuilder>();
        siloBuilder.Services.Returns(services);

        // Act
        ISiloBuilder result = siloBuilder.UseAqueduct();

        // Assert
        Assert.Same(siloBuilder, result);
    }

    /// <summary>
    ///     UseAqueduct with no action should throw when siloBuilder is null.
    /// </summary>
    [Fact]
    public void UseAqueductWithNoActionThrowsWhenSiloBuilderIsNull()
    {
        // Arrange
        ISiloBuilder siloBuilder = null!;

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => siloBuilder.UseAqueduct());
        Assert.Equal("siloBuilder", exception.ParamName);
    }
}