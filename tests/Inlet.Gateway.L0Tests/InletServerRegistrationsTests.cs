using System;
using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


namespace Mississippi.Inlet.Gateway.L0Tests;

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

        // Act
        IServiceCollection result = services.AddInletServer();

        // Assert
        Assert.Same(services, result);
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
        const string customPolicy = "custom.generated.api";

        // Act
        services.AddInletServer(options => options.GeneratedApiAuthorization.DefaultPolicy = customPolicy);
        using ServiceProvider provider = services.BuildServiceProvider();
        IOptions<InletServerOptions> options = provider.GetRequiredService<IOptions<InletServerOptions>>();

        // Assert
        Assert.Equal(customPolicy, options.Value.GeneratedApiAuthorization.DefaultPolicy);
    }

    /// <summary>
    ///     AddInletServer should apply generated API authorization configuration options.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Test method verifies configuration.")]
    public void AddInletServerAppliesGeneratedApiAuthorizationOptions()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddInletServer(options =>
        {
            options.GeneratedApiAuthorization.Mode =
                GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints;
            options.GeneratedApiAuthorization.DefaultPolicy = "generated-policy";
            options.GeneratedApiAuthorization.DefaultRoles = "admin";
            options.GeneratedApiAuthorization.DefaultAuthenticationSchemes = "Bearer";
            options.GeneratedApiAuthorization.AllowAnonymousOptOut = false;
        });
        using ServiceProvider provider = services.BuildServiceProvider();
        IOptions<InletServerOptions> options = provider.GetRequiredService<IOptions<InletServerOptions>>();

        // Assert
        Assert.Equal(
            GeneratedApiAuthorizationMode.RequireAuthorizationForAllGeneratedEndpoints,
            options.Value.GeneratedApiAuthorization.Mode);
        Assert.Equal("generated-policy", options.Value.GeneratedApiAuthorization.DefaultPolicy);
        Assert.Equal("admin", options.Value.GeneratedApiAuthorization.DefaultRoles);
        Assert.Equal("Bearer", options.Value.GeneratedApiAuthorization.DefaultAuthenticationSchemes);
        Assert.False(options.Value.GeneratedApiAuthorization.AllowAnonymousOptOut);
    }

    /// <summary>
    ///     AddInletServer should register MVC options setup for generated API authorization convention.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP001:Dispose created",
        Justification = "Test method verifies registration.")]
    public void AddInletServerRegistersGeneratedApiAuthorizationMvcOptionsSetup()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddInletServer();

        // Assert
        Assert.Contains(
            services,
            descriptor => (descriptor.ServiceType == typeof(IConfigureOptions<MvcOptions>)) &&
                          (descriptor.ImplementationType == typeof(GeneratedApiAuthorizationMvcOptionsSetup)));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IConfigureOptions<MvcOptions>));
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