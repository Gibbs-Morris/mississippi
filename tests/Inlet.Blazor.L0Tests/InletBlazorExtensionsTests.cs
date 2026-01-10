using System;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Inlet.Blazor.L0Tests;

/// <summary>
///     Tests for <see cref="InletBlazorExtensions" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Blazor")]
[AllureSuite("Extensions")]
[AllureSubSuite("InletBlazorExtensions")]
public sealed class InletBlazorExtensionsTests
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