using System;


using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        ServiceCollection services = new();
        services.AddSingleton<IGrainFactory>(new Mock<IGrainFactory>().Object);
        services.AddSingleton(new Mock<ILogger<SnapshotGrainFactory>>().Object);

        // Act
        services.AddSnapshotCaching();

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
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddSnapshotCaching();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     AddSnapshotCaching should throw when services is null.
    /// </summary>
    [Fact]
        public void AddSnapshotCachingThrowsWhenServicesIsNull()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddSnapshotCaching());
    }

    /// <summary>
    ///     AddSnapshotStateConverter should register ISnapshotStateConverter.
    /// </summary>
    [Fact]
        public void AddSnapshotStateConverterRegistersConverter()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddSingleton(new Mock<ISerializationProvider>().Object);

        // Act
        services.AddSnapshotStateConverter<TestState>();

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
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddSnapshotStateConverter<TestState>();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     AddSnapshotStateConverter should throw when services is null.
    /// </summary>
    [Fact]
        public void AddSnapshotStateConverterThrowsWhenServicesIsNull()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddSnapshotStateConverter<TestState>());
    }
}