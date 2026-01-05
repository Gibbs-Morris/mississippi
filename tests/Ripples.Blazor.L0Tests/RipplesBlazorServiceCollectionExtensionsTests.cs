using System;
using System.Diagnostics.CodeAnalysis;

using Allure.Xunit.Attributes;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Ripples.Abstractions;

using Moq;


namespace Mississippi.Ripples.Blazor.L0Tests;

/// <summary>
///     Tests for <see cref="RipplesBlazorServiceCollectionExtensions" />.
/// </summary>
[AllureParentSuite("Mississippi")]
[AllureSuite("Ripples.Blazor")]
[AllureSubSuite("RipplesBlazorServiceCollectionExtensions")]
public sealed class RipplesBlazorServiceCollectionExtensionsTests
{
    [SuppressMessage("SonarQube", "S2094:Classes should not be empty", Justification = "Test stub class.")]
    private sealed class TestProjection
    {
    }

    /// <summary>
    ///     Verifies that AddRipplePool registers the factory.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddRipplePoolRegistersFactory()
    {
        // Arrange
        ServiceCollection services = new();
        Mock<IRipplePool<TestProjection>> poolMock = new();

        // Act
        services.AddRipplePool<TestProjection>(_ => poolMock.Object);

        // Assert
        services.Should().ContainSingle(d => d.ServiceType == typeof(IRipplePool<TestProjection>));
    }

    /// <summary>
    ///     Verifies that AddRipplePool throws when factory is null.
    /// </summary>
    [Fact]
    [AllureFeature("Parameter Validation")]
    public void AddRipplePoolThrowsWhenFactoryIsNull()
    {
        // Arrange
        ServiceCollection services = new();
        Func<IServiceProvider, IRipplePool<TestProjection>>? factory = null;

        // Act
        Action act = () => services.AddRipplePool(factory!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("factory");
    }

    /// <summary>
    ///     Verifies that AddRipplePool throws when services is null.
    /// </summary>
    [Fact]
    [AllureFeature("Parameter Validation")]
    public void AddRipplePoolThrowsWhenServicesIsNull()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        Action act = () => services!.AddRipplePool<TestProjection>(_ => Mock.Of<IRipplePool<TestProjection>>());

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    /// <summary>
    ///     Verifies that AddRipple registers the factory.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddRippleRegistersFactory()
    {
        // Arrange
        ServiceCollection services = new();
        Mock<IRipple<TestProjection>> rippleMock = new();

        // Act
        services.AddRipple<TestProjection>(_ => rippleMock.Object);

        // Assert
        services.Should().ContainSingle(d => d.ServiceType == typeof(IRipple<TestProjection>));
    }

    /// <summary>
    ///     Verifies that AddRipple throws when factory is null.
    /// </summary>
    [Fact]
    [AllureFeature("Parameter Validation")]
    public void AddRippleThrowsWhenFactoryIsNull()
    {
        // Arrange
        ServiceCollection services = new();
        Func<IServiceProvider, IRipple<TestProjection>>? factory = null;

        // Act
        Action act = () => services.AddRipple(factory!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("factory");
    }

    /// <summary>
    ///     Verifies that AddRipple throws when services is null.
    /// </summary>
    [Fact]
    [AllureFeature("Parameter Validation")]
    public void AddRippleThrowsWhenServicesIsNull()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        Action act = () => services!.AddRipple<TestProjection>(_ => Mock.Of<IRipple<TestProjection>>());

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    /// <summary>
    ///     Verifies that AddRipplesBlazor returns the service collection for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddRipplesBlazorReturnsServiceCollectionForChaining()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddRipplesBlazor();

        // Assert
        result.Should().BeSameAs(services);
    }

    /// <summary>
    ///     Verifies that AddRipplesBlazor throws when services is null.
    /// </summary>
    [Fact]
    [AllureFeature("Parameter Validation")]
    public void AddRipplesBlazorThrowsWhenServicesIsNull()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        Action act = () => services!.AddRipplesBlazor();

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }
}