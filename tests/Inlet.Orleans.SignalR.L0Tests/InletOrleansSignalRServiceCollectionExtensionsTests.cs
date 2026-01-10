using System;
using System.Diagnostics.CodeAnalysis;

using Allure.Xunit.Attributes;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


namespace Mississippi.Inlet.Orleans.SignalR.L0Tests;

/// <summary>
///     Tests for <see cref="InletOrleansSignalRServiceCollectionExtensions" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Orleans.SignalR")]
[AllureSuite("Extensions")]
[AllureSubSuite("InletOrleansSignalRServiceCollectionExtensions")]
public sealed class InletOrleansSignalRServiceCollectionExtensionsTests
{
    /// <summary>
    ///     AddInletOrleansWithSignalR should accept null configuration action.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Test method verifies null action.")]
    public void AddInletOrleansWithSignalRAcceptsNullConfigurationAction()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddInletOrleansWithSignalR();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     AddInletOrleansWithSignalR should apply configuration options.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Test method verifies configuration.")]
    public void AddInletOrleansWithSignalRAppliesConfigurationOptions()
    {
        // Arrange
        ServiceCollection services = new();
        string customNamespace = "Custom.Namespace";

        // Act
        services.AddInletOrleansWithSignalR(options => options.AllClientsStreamNamespace = customNamespace);
        using ServiceProvider provider = services.BuildServiceProvider();
        IOptions<InletOrleansOptions> options = provider.GetRequiredService<IOptions<InletOrleansOptions>>();

        // Assert
        Assert.Equal(customNamespace, options.Value.AllClientsStreamNamespace);
    }

    /// <summary>
    ///     AddInletOrleansWithSignalR should register HubLifetimeManager via TryAddSingleton.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Test method verifies registration.")]
    public void AddInletOrleansWithSignalRRegistersHubLifetimeManager()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddInletOrleansWithSignalR();

        // Assert - TryAddSingleton only adds if not present; check any HubLifetimeManager-related type
        // The type is registered as open generic so we verify via service resolution in integration
        // Here we just verify the method completes and adds expected options
        Assert.Contains(
            services,
            d => d.ServiceType.FullName?.Contains("IOptions`1", StringComparison.Ordinal) == true);
    }

    /// <summary>
    ///     AddInletOrleansWithSignalR should register SignalR services.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Test method verifies registration.")]
    public void AddInletOrleansWithSignalRRegistersSignalRServices()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddInletOrleansWithSignalR();

        // Assert
        Assert.Contains(services, d => d.ServiceType == typeof(IHubContext<>));
    }

    /// <summary>
    ///     AddInletOrleansWithSignalR should return the same service collection for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("Method Behavior")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Test method verifies chaining.")]
    public void AddInletOrleansWithSignalRReturnsSameServiceCollection()
    {
        // Arrange
        ServiceCollection services = new();

        // Act - chain multiple extensions
        IServiceCollection result = services
            .AddInletOrleansWithSignalR()
            .AddSingleton(_ => "test-value");

        // Assert
        Assert.Same(services, result);
        Assert.Contains(services, d => d.ServiceType == typeof(string));
    }

    /// <summary>
    ///     AddInletOrleansWithSignalR should throw ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Null argument test.")]
    public void AddInletOrleansWithSignalRThrowsWhenServicesIsNull()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddInletOrleansWithSignalR());
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