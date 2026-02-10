using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Mississippi.Common.Abstractions.Builders;
using Mississippi.EventSourcing.Serialization.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;

using Moq;

using Orleans;


namespace Mississippi.EventSourcing.Snapshots.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotRegistrations" />.
/// </summary>
public sealed class SnapshotRegistrationsTests
{
    private sealed class TestMississippiSiloBuilder : IMississippiSiloBuilder
    {
        private readonly IServiceCollection services;

        public TestMississippiSiloBuilder(
            IServiceCollection services
        )
        {
            ArgumentNullException.ThrowIfNull(services);
            this.services = services;
        }

        public IMississippiSiloBuilder ConfigureOptions<TOptions>(
            Action<TOptions> configure
        )
            where TOptions : class
        {
            services.Configure(configure);
            return this;
        }

        public IMississippiSiloBuilder ConfigureServices(
            Action<IServiceCollection> configure
        )
        {
            configure(services);
            return this;
        }
    }

    /// <summary>
    ///     Test state for snapshot registration tests.
    /// </summary>
    private sealed record TestState(int Value);

    /// <summary>
    ///     AddSnapshotCaching should register ISnapshotGrainFactory.
    /// </summary>
    [Fact]
    public void AddSnapshotCachingRegistersSnapshotGrainFactory()
    {
        // Arrange
        ServiceCollection services = [];
        TestMississippiSiloBuilder builder = new(services);
        services.AddSingleton<IGrainFactory>(new Mock<IGrainFactory>().Object);
        services.AddSingleton(new Mock<ILogger<SnapshotGrainFactory>>().Object);

        // Act
        builder.AddSnapshotCaching();

        // Assert
        using ServiceProvider provider = services.BuildServiceProvider();
        ISnapshotGrainFactory? factory = provider.GetService<ISnapshotGrainFactory>();
        Assert.NotNull(factory);
        Assert.IsType<SnapshotGrainFactory>(factory);
    }

    /// <summary>
    ///     AddSnapshotCaching should return the service collection for chaining.
    /// </summary>
    [Fact]
    public void AddSnapshotCachingReturnsServiceCollection()
    {
        // Arrange
        TestMississippiSiloBuilder builder = new(new ServiceCollection());

        // Act
        IMississippiSiloBuilder result = builder.AddSnapshotCaching();

        // Assert
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     AddSnapshotCaching should throw when services is null.
    /// </summary>
    [Fact]
    public void AddSnapshotCachingThrowsWhenServicesIsNull()
    {
        // Arrange
        IMississippiSiloBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddSnapshotCaching());
    }

    /// <summary>
    ///     AddSnapshotStateConverter should register ISnapshotStateConverter.
    /// </summary>
    [Fact]
    public void AddSnapshotStateConverterRegistersConverter()
    {
        // Arrange
        ServiceCollection services = [];
        TestMississippiSiloBuilder builder = new(services);
        services.AddSingleton(new Mock<ISerializationProvider>().Object);

        // Act
        builder.AddSnapshotStateConverter<TestState>();

        // Assert
        using ServiceProvider provider = services.BuildServiceProvider();
        ISnapshotStateConverter<TestState>? converter = provider.GetService<ISnapshotStateConverter<TestState>>();
        Assert.NotNull(converter);
        Assert.IsType<SnapshotStateConverter<TestState>>(converter);
    }

    /// <summary>
    ///     AddSnapshotStateConverter should return the service collection for chaining.
    /// </summary>
    [Fact]
    public void AddSnapshotStateConverterReturnsServiceCollection()
    {
        // Arrange
        TestMississippiSiloBuilder builder = new(new ServiceCollection());

        // Act
        IMississippiSiloBuilder result = builder.AddSnapshotStateConverter<TestState>();

        // Assert
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     AddSnapshotStateConverter should throw when services is null.
    /// </summary>
    [Fact]
    public void AddSnapshotStateConverterThrowsWhenServicesIsNull()
    {
        // Arrange
        IMississippiSiloBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddSnapshotStateConverter<TestState>());
    }
}