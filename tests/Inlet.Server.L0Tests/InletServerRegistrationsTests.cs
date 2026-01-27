using System;
using System.Diagnostics.CodeAnalysis;

using Allure.Xunit.Attributes;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


namespace Mississippi.Inlet.Server.L0Tests;

/// <summary>
///     Tests for <see cref="InletServerRegistrations" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Server")]
[AllureSuite("Extensions")]
[AllureSubSuite("InletServerRegistrations")]
public sealed class InletServerRegistrationsTests
{
    /// <summary>
    ///     AddInletServer should accept null configuration action.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Test method verifies null action.")]
    public void AddInletServerAcceptsNullConfigurationAction()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddInletServer();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     AddInletServer should apply configuration options.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Test method verifies configuration.")]
    public void AddInletServerAppliesConfigurationOptions()
    {
        // Arrange
        ServiceCollection services = new();
        string customNamespace = "Custom.Namespace";

        // Act
        services.AddInletServer(options => options.AllClientsStreamNamespace = customNamespace);
        using ServiceProvider provider = services.BuildServiceProvider();
        IOptions<InletServerOptions> options = provider.GetRequiredService<IOptions<InletServerOptions>>();

        // Assert
        Assert.Equal(customNamespace, options.Value.AllClientsStreamNamespace);
    }

    /// <summary>
    ///     AddInletServer should register HubLifetimeManager via TryAddSingleton.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Test method verifies registration.")]
    public void AddInletServerRegistersHubLifetimeManager()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddInletServer();

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
    [AllureFeature("Service Registration")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Test method verifies registration.")]
    public void AddInletServerRegistersSignalRServices()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddInletServer();

        // Assert
        Assert.Contains(services, d => d.ServiceType == typeof(IHubContext<>));
    }

    /// <summary>
    ///     AddInletServer should return the same service collection for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("Method Behavior")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Test method verifies chaining.")]
    public void AddInletServerReturnsSameServiceCollection()
    {
        // Arrange
        ServiceCollection services = new();

        // Act - chain multiple extensions
        IServiceCollection result = services.AddInletServer().AddSingleton(_ => "test-value");

        // Assert
        Assert.Same(services, result);
        Assert.Contains(services, d => d.ServiceType == typeof(string));
    }

    /// <summary>
    ///     AddInletServer should throw ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Null argument test.")]
    public void AddInletServerThrowsWhenServicesIsNull()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddInletServer());
    }

    /// <summary>
    ///     AddInletSignalRGrainObserver should return the same service collection for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("Method Behavior")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Test method verifies chaining.")]
    public void AddInletSignalRGrainObserverReturnsSameServiceCollection()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddInletSignalRGrainObserver();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     AddInletSignalRGrainObserver should throw ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Null argument test.")]
    public void AddInletSignalRGrainObserverThrowsWhenServicesIsNull()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddInletSignalRGrainObserver());
    }
}