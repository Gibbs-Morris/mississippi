using System;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.Brooks.Serialization.Abstractions;
using Mississippi.Tributary.Abstractions;

using Moq;

using Orleans;


namespace Mississippi.Tributary.Runtime.L0Tests;

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
    ///     Verifies the explicit scalar overload configures the same options contract.
    /// </summary>
    [Fact]
    public void AddSnapshotCachingAppliesExplicitScalarOptions()
    {
        ServiceCollection services = new();
        services.AddSnapshotCaching(20, true);
        using ServiceProvider provider = services.BuildServiceProvider();
        SnapshotRetentionOptions options = provider.GetRequiredService<IOptions<SnapshotRetentionOptions>>().Value;
        Assert.Equal(20, options.DefaultRetainModulus);
        Assert.True(options.ShouldPersistAllSnapshots);
    }

    /// <summary>
    ///     Verifies programmatic retention options are applied.
    /// </summary>
    [Fact]
    public void AddSnapshotCachingAppliesProgrammaticRetentionOptions()
    {
        ServiceCollection services = new();
        services.AddSnapshotCaching(options =>
        {
            options.DefaultRetainModulus = 20;
            options.ShouldPersistAllSnapshots = true;
            options.StateTypeOverrides["STATE.V1"] = 10;
        });
        using ServiceProvider provider = services.BuildServiceProvider();
        SnapshotRetentionOptions options = provider.GetRequiredService<IOptions<SnapshotRetentionOptions>>().Value;
        Assert.Equal(20, options.DefaultRetainModulus);
        Assert.True(options.ShouldPersistAllSnapshots);
        Assert.Equal(10, options.StateTypeOverrides["STATE.V1"]);
    }

    /// <summary>
    ///     Verifies configuration binding populates all retention option properties.
    /// </summary>
    [Fact]
    public void AddSnapshotCachingBindsConfigurationSection()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [nameof(SnapshotRetentionOptions.DefaultRetainModulus)] = "20",
                    [nameof(SnapshotRetentionOptions.ShouldPersistAllSnapshots)] = "true",
                    [$"{nameof(SnapshotRetentionOptions.StateTypeOverrides)}:STATE.V1"] = "10",
                })
            .Build();
        ServiceCollection services = new();
        services.AddSnapshotCaching(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();
        SnapshotRetentionOptions options = provider.GetRequiredService<IOptions<SnapshotRetentionOptions>>().Value;
        Assert.Equal(20, options.DefaultRetainModulus);
        Assert.True(options.ShouldPersistAllSnapshots);
        Assert.Equal(10, options.StateTypeOverrides["STATE.V1"]);
    }

    /// <summary>
    ///     Verifies default retention options and the startup policy service are registered.
    /// </summary>
    [Fact]
    public void AddSnapshotCachingRegistersDefaultRetentionPolicy()
    {
        ServiceCollection services = new();
        services.AddSnapshotCaching();
        using ServiceProvider provider = services.BuildServiceProvider();
        SnapshotRetentionOptions options = provider.GetRequiredService<IOptions<SnapshotRetentionOptions>>().Value;
        Assert.Equal(50, options.DefaultRetainModulus);
        Assert.False(options.ShouldPersistAllSnapshots);
        Assert.Contains(
            services,
            descriptor => (descriptor.ServiceType == typeof(IHostedService)) &&
                          (descriptor.ImplementationType == typeof(SnapshotRetentionPolicyStartupService)));
    }

    /// <summary>
    ///     AddSnapshotCaching should register ISnapshotGrainFactory.
    /// </summary>
    [Fact]
    public void AddSnapshotCachingRegistersSnapshotGrainFactory()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddSingleton(new Mock<IGrainFactory>().Object);
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
    ///     Verifies that blank state type override keys fail options validation.
    /// </summary>
    [Fact]
    public void AddSnapshotCachingRejectsBlankOverrideKey()
    {
        ServiceCollection services = new();
        services.AddSnapshotCaching(options => options.StateTypeOverrides[" "] = 10);
        using ServiceProvider provider = services.BuildServiceProvider();
        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<SnapshotRetentionOptions>>().Value);
        Assert.Contains("empty state type key", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies that an invalid default interval fails options validation.
    /// </summary>
    [Fact]
    public void AddSnapshotCachingRejectsInvalidDefaultInterval()
    {
        ServiceCollection services = new();
        services.AddSnapshotCaching(options => options.DefaultRetainModulus = 0);
        using ServiceProvider provider = services.BuildServiceProvider();
        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<SnapshotRetentionOptions>>().Value);
        Assert.Contains(
            nameof(SnapshotRetentionOptions.DefaultRetainModulus),
            exception.Message,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies that invalid overrides fail validation even when save-all is enabled.
    /// </summary>
    [Fact]
    public void AddSnapshotCachingRejectsInvalidOverrideWhenSaveAllIsEnabled()
    {
        ServiceCollection services = new();
        services.AddSnapshotCaching(options =>
        {
            options.ShouldPersistAllSnapshots = true;
            options.StateTypeOverrides["STATE.V1"] = 0;
        });
        using ServiceProvider provider = services.BuildServiceProvider();
        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<SnapshotRetentionOptions>>().Value);
        Assert.Contains("STATE.V1", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies null overload arguments are rejected.
    /// </summary>
    [Fact]
    public void AddSnapshotCachingRejectsNullOverloadArguments()
    {
        ServiceCollection services = new();
        Assert.Throws<ArgumentNullException>(() =>
            services.AddSnapshotCaching((Action<SnapshotRetentionOptions>)null!));
        Assert.Throws<ArgumentNullException>(() => services.AddSnapshotCaching((IConfiguration)null!));
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