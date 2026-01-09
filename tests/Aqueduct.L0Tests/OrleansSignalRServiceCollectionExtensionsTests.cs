using System;
using System.Linq;

using Allure.Xunit.Attributes;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;


namespace Mississippi.Aqueduct.L0Tests;

/// <summary>
///     Tests for <see cref="OrleansSignalRServiceCollectionExtensions" />.
/// </summary>
[AllureParentSuite("ASP.NET Core")]
[AllureSuite("SignalR Orleans")]
[AllureSubSuite("Service Collection Extensions")]
public sealed class OrleansSignalRServiceCollectionExtensionsTests
{
    private sealed class TestHub : Hub;

    /// <summary>
    ///     Tests that AddOrleansSignalR throws when services is null.
    /// </summary>
    [Fact(DisplayName = "AddOrleansSignalR Throws When Services Is Null")]
    public void AddOrleansSignalRShouldThrowWhenServicesIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            OrleansSignalRServiceCollectionExtensions.AddOrleansSignalR<TestHub>(null!));
    }

    /// <summary>
    ///     Tests that AddOrleansSignalR registers HubLifetimeManager.
    /// </summary>
    [Fact(DisplayName = "AddOrleansSignalR Registers HubLifetimeManager")]
    public void AddOrleansSignalRShouldRegisterHubLifetimeManager()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddOrleansSignalR<TestHub>();

        // Assert
        ServiceDescriptor? descriptor =
            services.FirstOrDefault(d => d.ServiceType == typeof(HubLifetimeManager<TestHub>));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(OrleansHubLifetimeManager<TestHub>), descriptor.ImplementationType);
    }

    /// <summary>
    ///     Tests that AddOrleansSignalR returns services for chaining.
    /// </summary>
    [Fact(DisplayName = "AddOrleansSignalR Returns Services For Chaining")]
    public void AddOrleansSignalRShouldReturnServicesForChaining()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddOrleansSignalR<TestHub>();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     Tests that AddOrleansSignalR with options throws when services is null.
    /// </summary>
    [Fact(DisplayName = "AddOrleansSignalR With Options Throws When Services Is Null")]
    public void AddOrleansSignalRWithOptionsShouldThrowWhenServicesIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            OrleansSignalRServiceCollectionExtensions.AddOrleansSignalR<TestHub>(null!, _ => { }));
    }

    /// <summary>
    ///     Tests that AddOrleansSignalR with options throws when configureOptions is null.
    /// </summary>
    [Fact(DisplayName = "AddOrleansSignalR With Options Throws When ConfigureOptions Is Null")]
    public void AddOrleansSignalRWithOptionsShouldThrowWhenConfigureOptionsIsNull()
    {
        // Arrange
        ServiceCollection services = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            services.AddOrleansSignalR<TestHub>(null!));
    }

    /// <summary>
    ///     Tests that AddOrleansSignalR with options registers HubLifetimeManager.
    /// </summary>
    [Fact(DisplayName = "AddOrleansSignalR With Options Registers HubLifetimeManager")]
    public void AddOrleansSignalRWithOptionsShouldRegisterHubLifetimeManager()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddOrleansSignalR<TestHub>(options => options.StreamProviderName = "CustomProvider");

        // Assert
        ServiceDescriptor? descriptor =
            services.FirstOrDefault(d => d.ServiceType == typeof(HubLifetimeManager<TestHub>));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    /// <summary>
    ///     Tests that AddOrleansSignalR with options configures options when resolved.
    /// </summary>
    [Fact(DisplayName = "AddOrleansSignalR With Options Configures Options")]
    public void AddOrleansSignalRWithOptionsShouldConfigureOptions()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddOrleansSignalR<TestHub>(options =>
        {
            options.StreamProviderName = "CustomProvider";
        });

        // Build provider and resolve options to trigger configuration
        using ServiceProvider provider = services.BuildServiceProvider();
        IOptions<OrleansSignalROptions>? resolvedOptions = provider.GetService<IOptions<OrleansSignalROptions>>();

        // Assert - configuration action is applied when options are resolved
        Assert.NotNull(resolvedOptions);
        Assert.Equal("CustomProvider", resolvedOptions.Value.StreamProviderName);
    }

    /// <summary>
    ///     Tests that AddOrleansSignalR with options returns services for chaining.
    /// </summary>
    [Fact(DisplayName = "AddOrleansSignalR With Options Returns Services For Chaining")]
    public void AddOrleansSignalRWithOptionsShouldReturnServicesForChaining()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddOrleansSignalR<TestHub>(_ => { });

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     Tests that AddOrleansSignalRGrainObserver throws when services is null.
    /// </summary>
    [Fact(DisplayName = "AddOrleansSignalRGrainObserver Throws When Services Is Null")]
    public void AddOrleansSignalRGrainObserverShouldThrowWhenServicesIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            OrleansSignalRServiceCollectionExtensions.AddOrleansSignalRGrainObserver(null!));
    }

    /// <summary>
    ///     Tests that AddOrleansSignalRGrainObserver registers ISignalRGrainObserver.
    /// </summary>
    [Fact(DisplayName = "AddOrleansSignalRGrainObserver Registers ISignalRGrainObserver")]
    public void AddOrleansSignalRGrainObserverShouldRegisterGrainObserver()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddOrleansSignalRGrainObserver();

        // Assert
        ServiceDescriptor? descriptor =
            services.FirstOrDefault(d => d.ServiceType == typeof(ISignalRGrainObserver));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(OrleansSignalRGrainObserver), descriptor.ImplementationType);
    }

    /// <summary>
    ///     Tests that AddOrleansSignalRGrainObserver returns services for chaining.
    /// </summary>
    [Fact(DisplayName = "AddOrleansSignalRGrainObserver Returns Services For Chaining")]
    public void AddOrleansSignalRGrainObserverShouldReturnServicesForChaining()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddOrleansSignalRGrainObserver();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     Tests that TryAdd semantics are used (second registration is ignored).
    /// </summary>
    [Fact(DisplayName = "AddOrleansSignalR Uses TryAdd Semantics")]
    public void AddOrleansSignalRShouldUseTryAddSemantics()
    {
        // Arrange
        ServiceCollection services = new();

        // Act - add twice
        services.AddOrleansSignalR<TestHub>();
        services.AddOrleansSignalR<TestHub>();

        // Assert - should only have one registration
        int count = services.Count(d => d.ServiceType == typeof(HubLifetimeManager<TestHub>));
        Assert.Equal(1, count);
    }

    /// <summary>
    ///     Tests that TryAdd semantics are used for grain observer.
    /// </summary>
    [Fact(DisplayName = "AddOrleansSignalRGrainObserver Uses TryAdd Semantics")]
    public void AddOrleansSignalRGrainObserverShouldUseTryAddSemantics()
    {
        // Arrange
        ServiceCollection services = new();

        // Act - add twice
        services.AddOrleansSignalRGrainObserver();
        services.AddOrleansSignalRGrainObserver();

        // Assert - should only have one registration
        int count = services.Count(d => d.ServiceType == typeof(ISignalRGrainObserver));
        Assert.Equal(1, count);
    }
}
