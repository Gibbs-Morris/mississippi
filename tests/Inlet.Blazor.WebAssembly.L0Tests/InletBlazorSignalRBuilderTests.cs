using System;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Inlet.Blazor.WebAssembly.L0Tests;

/// <summary>
///     Tests for <see cref="InletBlazorSignalRBuilder" /> behavior.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Blazor.WebAssembly")]
[AllureSuite("Core")]
[AllureSubSuite("InletBlazorSignalRBuilder")]
public sealed class InletBlazorSignalRBuilderTests
{
    /// <summary>
    ///     Constructor should succeed with valid services collection.
    /// </summary>
    [Fact]
    [AllureFeature("Construction")]
    public void ConstructorSucceedsWithValidServices()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        InletBlazorSignalRBuilder builder = new(services);

        // Assert
        Assert.NotNull(builder);
    }

    /// <summary>
    ///     Constructor should throw when services is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void ConstructorThrowsWhenServicesIsNull()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new InletBlazorSignalRBuilder(null!));
    }

    /// <summary>
    ///     ScanProjectionDtos should return the builder for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("Builder Pattern")]
    public void ScanProjectionDtosReturnsBuilderForChaining()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act
        InletBlazorSignalRBuilder result = builder.ScanProjectionDtos(typeof(InletBlazorSignalRBuilderTests).Assembly);

        // Assert
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     ScanProjectionDtos should throw when assemblies is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void ScanProjectionDtosThrowsWhenAssembliesIsNull()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.ScanProjectionDtos(null!));
    }

    /// <summary>
    ///     WithHubPath should return the builder for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("Builder Pattern")]
    public void WithHubPathReturnsBuilderForChaining()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act
        InletBlazorSignalRBuilder result = builder.WithHubPath("/hubs/test");

        // Assert
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     WithHubPath should throw when hubPath is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void WithHubPathThrowsWhenHubPathIsNull()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => builder.WithHubPath(null!));
    }

    /// <summary>
    ///     WithHubPath should throw when hubPath is whitespace.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void WithHubPathThrowsWhenHubPathIsWhitespace()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithHubPath("   "));
    }

    /// <summary>
    ///     WithRoutePrefix should return the builder for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("Builder Pattern")]
    public void WithRoutePrefixReturnsBuilderForChaining()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act
        InletBlazorSignalRBuilder result = builder.WithRoutePrefix("/api/projections");

        // Assert
        Assert.Same(builder, result);
    }

    /// <summary>
    ///     WithRoutePrefix should throw when prefix is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void WithRoutePrefixThrowsWhenPrefixIsNull()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => builder.WithRoutePrefix(null!));
    }

    /// <summary>
    ///     WithRoutePrefix should throw when prefix is whitespace.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void WithRoutePrefixThrowsWhenPrefixIsWhitespace()
    {
        // Arrange
        ServiceCollection services = new();
        InletBlazorSignalRBuilder builder = new(services);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithRoutePrefix("   "));
    }
}