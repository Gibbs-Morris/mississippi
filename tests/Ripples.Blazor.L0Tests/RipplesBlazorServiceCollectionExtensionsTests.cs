using System;
using System.Linq;

using Allure.Xunit.Attributes;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Ripples.Abstractions;


namespace Mississippi.Ripples.Blazor.L0Tests;

/// <summary>
///     Tests for <see cref="RipplesBlazorServiceCollectionExtensions" />.
/// </summary>
[AllureParentSuite("Mississippi")]
[AllureSuite("Ripples.Blazor")]
[AllureSubSuite("RipplesBlazorServiceCollectionExtensions")]
public sealed class RipplesBlazorServiceCollectionExtensionsTests
{
    /// <summary>
    ///     Verifies that AddRippleStore invokes the configure action.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddRippleStoreInvokesConfigureAction()
    {
        // Arrange
        ServiceCollection services = new();
        bool configureInvoked = false;

        // Act
        services.AddRippleStore(_ => configureInvoked = true);
        using ServiceProvider provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IRippleStore>();

        // Assert
        configureInvoked.Should().BeTrue();
    }

    /// <summary>
    ///     Verifies that AddRippleStore registers IRippleStore as scoped.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddRippleStoreRegistersAsScoped()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddRippleStore();

        // Assert
        ServiceDescriptor? descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRippleStore));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    /// <summary>
    ///     Verifies that AddRippleStore registers IRippleStore.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddRippleStoreRegistersIRippleStore()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddRippleStore();

        // Assert
        services.Should().ContainSingle(d => d.ServiceType == typeof(IRippleStore));
    }

    /// <summary>
    ///     Verifies that AddRippleStore returns the service collection for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddRippleStoreReturnsServiceCollectionForChaining()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddRippleStore();

        // Assert
        result.Should().BeSameAs(services);
    }

    /// <summary>
    ///     Verifies that AddRippleStore throws when services is null.
    /// </summary>
    [Fact]
    [AllureFeature("Parameter Validation")]
    public void AddRippleStoreThrowsWhenServicesIsNull()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        Action act = () => services!.AddRippleStore();

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }
}