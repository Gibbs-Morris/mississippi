using System;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Inlet.Client.L0Tests.Registrations;

/// <summary>
///     Tests for <see cref="InletBlazorRegistrations" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet")]
[AllureSuite("Registrations")]
[AllureSubSuite("InletBlazorRegistrations")]
public sealed class InletBlazorRegistrationsTests
{
    /// <summary>
    ///     AddInletBlazor returns the service collection for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("Registration")]
    public void AddInletBlazorReturnsServiceCollection()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddInletBlazor();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     AddInletBlazorSignalR accepts null configure action.
    /// </summary>
    [Fact]
    [AllureFeature("SignalR")]
    public void AddInletBlazorSignalRAcceptsNullConfigure()
    {
        // Arrange
        ServiceCollection services = new();

        // Act (should not throw)
        IServiceCollection result = services.AddInletBlazorSignalR();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     AddInletBlazorSignalR invokes configure action.
    /// </summary>
    [Fact]
    [AllureFeature("SignalR")]
    public void AddInletBlazorSignalRInvokesConfigureAction()
    {
        // Arrange
        ServiceCollection services = new();
        bool configureWasCalled = false;

        // Act
        services.AddInletBlazorSignalR(builder => { configureWasCalled = true; });

        // Assert
        Assert.True(configureWasCalled);
    }

    /// <summary>
    ///     AddInletBlazorSignalR returns the service collection for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("SignalR")]
    public void AddInletBlazorSignalRReturnsServiceCollection()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddInletBlazorSignalR();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     AddInletBlazorSignalR throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    [AllureFeature("SignalR")]
    public void AddInletBlazorSignalRThrowsWhenServicesIsNull()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddInletBlazorSignalR());
    }

    /// <summary>
    ///     AddInletBlazor throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    [AllureFeature("Registration")]
    public void AddInletBlazorThrowsWhenServicesIsNull()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddInletBlazor());
    }
}