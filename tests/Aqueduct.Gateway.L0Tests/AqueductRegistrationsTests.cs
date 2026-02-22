using System;
using System.Linq;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.Aqueduct.Abstractions;


namespace Mississippi.Aqueduct.L0Tests;

/// <summary>
///     Tests for <see cref="AqueductRegistrations" />.
/// </summary>
public sealed class AqueductRegistrationsTests
{
    private sealed class TestHub : Hub;

    /// <summary>
    ///     Tests that AddAqueductNotifier registers IAqueductNotifier.
    /// </summary>
    [Fact(DisplayName = "AddAqueductNotifier Registers IAqueductNotifier")]
    public void AddAqueductNotifierShouldRegisterNotifier()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddAqueductNotifier();

        // Assert
        ServiceDescriptor? descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAqueductNotifier));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(AqueductNotifier), descriptor.ImplementationType);
    }

    /// <summary>
    ///     Tests that AddAqueductNotifier returns services for chaining.
    /// </summary>
    [Fact(DisplayName = "AddAqueductNotifier Returns Services For Chaining")]
    public void AddAqueductNotifierShouldReturnServicesForChaining()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddAqueductNotifier();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     Tests that AddAqueductNotifier throws when services is null.
    /// </summary>
    [Fact(DisplayName = "AddAqueductNotifier Throws When Services Is Null")]
    public void AddAqueductNotifierShouldThrowWhenServicesIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => AqueductRegistrations.AddAqueductNotifier(null!));
    }

    /// <summary>
    ///     Tests that TryAdd semantics are used for notifier.
    /// </summary>
    [Fact(DisplayName = "AddAqueductNotifier Uses TryAdd Semantics")]
    public void AddAqueductNotifierShouldUseTryAddSemantics()
    {
        // Arrange
        ServiceCollection services = new();

        // Act - add twice
        services.AddAqueductNotifier();
        services.AddAqueductNotifier();

        // Assert - should only have one registration
        int count = services.Count(d => d.ServiceType == typeof(IAqueductNotifier));
        Assert.Equal(1, count);
    }

    /// <summary>
    ///     Tests that AddAqueduct registers HubLifetimeManager.
    /// </summary>
    [Fact(DisplayName = "AddAqueduct Registers HubLifetimeManager")]
    public void AddAqueductShouldRegisterHubLifetimeManager()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddAqueduct<TestHub>();

        // Assert
        ServiceDescriptor? descriptor =
            services.FirstOrDefault(d => d.ServiceType == typeof(HubLifetimeManager<TestHub>));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(AqueductHubLifetimeManager<TestHub>), descriptor.ImplementationType);
    }

    /// <summary>
    ///     Tests that AddAqueduct returns services for chaining.
    /// </summary>
    [Fact(DisplayName = "AddAqueduct Returns Services For Chaining")]
    public void AddAqueductShouldReturnServicesForChaining()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddAqueduct<TestHub>();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     Tests that AddAqueduct throws when services is null.
    /// </summary>
    [Fact(DisplayName = "AddAqueduct Throws When Services Is Null")]
    public void AddAqueductShouldThrowWhenServicesIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => AqueductRegistrations.AddAqueduct<TestHub>(null!));
    }

    /// <summary>
    ///     Tests that TryAdd semantics are used (second registration is ignored).
    /// </summary>
    [Fact(DisplayName = "AddAqueduct Uses TryAdd Semantics")]
    public void AddAqueductShouldUseTryAddSemantics()
    {
        // Arrange
        ServiceCollection services = new();

        // Act - add twice
        services.AddAqueduct<TestHub>();
        services.AddAqueduct<TestHub>();

        // Assert - should only have one registration
        int count = services.Count(d => d.ServiceType == typeof(HubLifetimeManager<TestHub>));
        Assert.Equal(1, count);
    }

    /// <summary>
    ///     Tests that AddAqueduct with options configures options when resolved.
    /// </summary>
    [Fact(DisplayName = "AddAqueduct With Options Configures Options")]
    public void AddAqueductWithOptionsShouldConfigureOptions()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddAqueduct<TestHub>(options => { options.StreamProviderName = "CustomProvider"; });

        // Build provider and resolve options to trigger configuration
        using ServiceProvider provider = services.BuildServiceProvider();
        IOptions<AqueductOptions>? resolvedOptions = provider.GetService<IOptions<AqueductOptions>>();

        // Assert - configuration action is applied when options are resolved
        Assert.NotNull(resolvedOptions);
        Assert.Equal("CustomProvider", resolvedOptions.Value.StreamProviderName);
    }

    /// <summary>
    ///     Tests that AddAqueduct with options registers HubLifetimeManager.
    /// </summary>
    [Fact(DisplayName = "AddAqueduct With Options Registers HubLifetimeManager")]
    public void AddAqueductWithOptionsShouldRegisterHubLifetimeManager()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddAqueduct<TestHub>(options => options.StreamProviderName = "CustomProvider");

        // Assert
        ServiceDescriptor? descriptor =
            services.FirstOrDefault(d => d.ServiceType == typeof(HubLifetimeManager<TestHub>));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    /// <summary>
    ///     Tests that AddAqueduct with options returns services for chaining.
    /// </summary>
    [Fact(DisplayName = "AddAqueduct With Options Returns Services For Chaining")]
    public void AddAqueductWithOptionsShouldReturnServicesForChaining()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddAqueduct<TestHub>(_ => { });

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     Tests that AddAqueduct with options throws when configureOptions is null.
    /// </summary>
    [Fact(DisplayName = "AddAqueduct With Options Throws When ConfigureOptions Is Null")]
    public void AddAqueductWithOptionsShouldThrowWhenConfigureOptionsIsNull()
    {
        // Arrange
        ServiceCollection services = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddAqueduct<TestHub>(null!));
    }

    /// <summary>
    ///     Tests that AddAqueduct with options throws when services is null.
    /// </summary>
    [Fact(DisplayName = "AddAqueduct With Options Throws When Services Is Null")]
    public void AddAqueductWithOptionsShouldThrowWhenServicesIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => AqueductRegistrations.AddAqueduct<TestHub>(null!, _ => { }));
    }
}