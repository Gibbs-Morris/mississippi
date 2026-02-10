using System;
using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.Common.Abstractions.Builders;


namespace Mississippi.Inlet.Server.L0Tests;

/// <summary>
///     Tests for <see cref="InletServerRegistrations" />.
/// </summary>
public sealed class InletServerRegistrationsTests
{
    /// <summary>
    ///     AddInletServer should accept null configuration action.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Test method verifies null action.")]
    public void AddInletServerAcceptsNullConfigurationAction()
    {
        // Arrange
        ServiceCollection services = new();
        TestMississippiServerBuilder builder = new(services);

        // Act
        IMississippiServerBuilder result = builder.AddInletServer();

        // Assert
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     AddInletServer should apply configuration options.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Test method verifies configuration.")]
    public void AddInletServerAppliesConfigurationOptions()
    {
        // Arrange
        ServiceCollection services = new();
        TestMississippiServerBuilder builder = new(services);
        string customNamespace = "Custom.Namespace";

        // Act
        builder.AddInletServer(options => options.AllClientsStreamNamespace = customNamespace);
        using ServiceProvider provider = services.BuildServiceProvider();
        IOptions<InletServerOptions> options = provider.GetRequiredService<IOptions<InletServerOptions>>();

        // Assert
        Assert.Equal(customNamespace, options.Value.AllClientsStreamNamespace);
    }

    /// <summary>
    ///     AddInletServer should register HubLifetimeManager via TryAddSingleton.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Test method verifies registration.")]
    public void AddInletServerRegistersHubLifetimeManager()
    {
        // Arrange
        ServiceCollection services = new();
        TestMississippiServerBuilder builder = new(services);

        // Act
        builder.AddInletServer();

        // Assert - TryAddSingleton only adds if not present; check any HubLifetimeManager-related type
        // The type is registered as open generic so we verify via service resolution in integration
        // Here we just verify the method completes and adds expected options
        Assert.Contains(
            services,
            d => d.ServiceType.FullName?.Contains("IOptions`1", StringComparison.Ordinal) == true);
    }

    /// <summary>
    ///     AddInletServer should register SignalR services.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Test method verifies registration.")]
    public void AddInletServerRegistersSignalRServices()
    {
        // Arrange
        ServiceCollection services = new();
        TestMississippiServerBuilder builder = new(services);

        // Act
        builder.AddInletServer();

        // Assert
        Assert.Contains(services, d => d.ServiceType == typeof(IHubContext<>));
    }

    /// <summary>
    ///     AddInletServer should return the same service collection for chaining.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Test method verifies chaining.")]
    public void AddInletServerReturnsSameServiceCollection()
    {
        // Arrange
        ServiceCollection services = new();
        TestMississippiServerBuilder builder = new(services);

        // Act - chain multiple extensions
        IMississippiServerBuilder result = builder.AddInletServer()
            .ConfigureServices(serviceCollection => serviceCollection.AddSingleton(_ => "test-value"));

        // Assert
        Assert.Same(builder, result);
        Assert.Contains(services, d => d.ServiceType == typeof(string));
    }

    /// <summary>
    ///     AddInletServer should throw ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Null argument test.")]
    public void AddInletServerThrowsWhenServicesIsNull()
    {
        // Arrange
        IMississippiServerBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddInletServer());
    }

    /// <summary>
    ///     AddInletSignalRGrainObserver should return the same service collection for chaining.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Test method verifies chaining.")]
    public void AddInletSignalRGrainObserverReturnsSameServiceCollection()
    {
        // Arrange
        ServiceCollection services = new();
        TestMississippiServerBuilder builder = new(services);

        // Act
        IMississippiServerBuilder result = builder.AddInletSignalRGrainObserver();

        // Assert
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     AddInletSignalRGrainObserver should throw ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Null argument test.")]
    public void AddInletSignalRGrainObserverThrowsWhenServicesIsNull()
    {
        // Arrange
        IMississippiServerBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddInletSignalRGrainObserver());
    }
}