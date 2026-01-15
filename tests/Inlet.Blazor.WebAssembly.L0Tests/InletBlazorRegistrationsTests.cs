using System;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Inlet.Blazor.WebAssembly.L0Tests;

/// <summary>
///     Tests for <see cref="InletBlazorRegistrations" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Blazor.WebAssembly")]
[AllureSuite("Extensions")]
[AllureSubSuite("InletBlazorRegistrations")]
public sealed class InletBlazorRegistrationsTests
{
    /// <summary>
    ///     AddInletBlazor can be called multiple times without error.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddInletBlazorCanBeCalledMultipleTimes()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        IServiceCollection result1 = services.AddInletBlazor();
        IServiceCollection result2 = services.AddInletBlazor();

        // Assert
        Assert.Same(services, result1);
        Assert.Same(services, result2);
    }

    /// <summary>
    ///     AddInletBlazor should return the same services collection for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddInletBlazorReturnsSameCollection()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        IServiceCollection result = services.AddInletBlazor();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     AddInletBlazorSignalR can be called without configure action.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddInletBlazorSignalRCanBeCalledWithoutConfigureAction()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        IServiceCollection result = services.AddInletBlazorSignalR();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     AddInletBlazorSignalR should invoke the configure action when provided.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddInletBlazorSignalRInvokesConfigureAction()
    {
        // Arrange
        ServiceCollection services = [];
        bool configureInvoked = false;

        // Act
        services.AddInletBlazorSignalR(builder => { configureInvoked = true; });

        // Assert
        Assert.True(configureInvoked);
    }

    /// <summary>
    ///     AddInletBlazorSignalR should return the same services collection for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddInletBlazorSignalRReturnsSameCollection()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        IServiceCollection result = services.AddInletBlazorSignalR();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     AddInletBlazorSignalR should throw when services is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void AddInletBlazorSignalRThrowsWhenServicesNull()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => services.AddInletBlazorSignalR());
        Assert.Equal("services", exception.ParamName);
    }

    /// <summary>
    ///     AddInletBlazor should throw when services is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void AddInletBlazorThrowsWhenServicesNull()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => services.AddInletBlazor());
        Assert.Equal("services", exception.ParamName);
    }
}