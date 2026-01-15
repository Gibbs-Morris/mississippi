using System;
using System.Reflection;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.EventSourcing.UxProjections.Abstractions;
using Mississippi.EventSourcing.UxProjections.Abstractions.Attributes;
using Mississippi.Inlet.Projection.Abstractions;

using Moq;


namespace Mississippi.EventSourcing.UxProjections.Api.L0Tests;

/// <summary>
///     Tests for <see cref="UxProjectionEndpointExtensions" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("UX Projections API")]
[AllureSubSuite("UxProjectionEndpointExtensions")]
public sealed class UxProjectionEndpointExtensionsTests
{
    private const string TestProjectionRoute = "test-projections";

    private const string TestRoutePrefix = "api/projections";

    /// <summary>
    ///     Test projection DTO missing ProjectionPath attribute (should be skipped during scanning).
    /// </summary>
    [UxProjection]
    internal sealed record ProjectionWithoutPathAttribute(string Data);

    /// <summary>
    ///     Test projection DTO missing UxProjection attribute (should be skipped during scanning).
    /// </summary>
    [ProjectionPath("incomplete-projection")]
    internal sealed record ProjectionWithoutUxAttribute(string Data);

    /// <summary>
    ///     Test projection DTO with both required attributes.
    /// </summary>
    [ProjectionPath("test-projections")]
    [UxProjection]
    internal sealed record TestProjection(string Name, int Value);

    /// <summary>
    ///     Verifies that MapUxProjection registers endpoints with correct route pattern.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("MapUxProjection")]
    public async Task MapUxProjectionRegistersEndpointsWithCorrectRoute()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        builder.Services.AddSingleton(factoryMock.Object);
        builder.Services.AddSingleton<ILogger<TestProjection>>(NullLogger<TestProjection>.Instance);
        WebApplication app = builder.Build();

        // Act
        RouteGroupBuilder group = app.MapUxProjection<TestProjection>(TestProjectionRoute);

        // Assert
        Assert.NotNull(group);
        await app.DisposeAsync();
    }

    /// <summary>
    ///     Verifies that MapUxProjection with single route parameter throws when endpoints is null.
    /// </summary>
    [Fact]
    [AllureFeature("MapUxProjection")]
    public void MapUxProjectionThrowsWhenEndpointsIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            UxProjectionEndpointExtensions.MapUxProjection<TestProjection>(null!, TestProjectionRoute));
    }

    /// <summary>
    ///     Verifies that MapUxProjection with single route parameter throws when route is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("MapUxProjection")]
    public async Task MapUxProjectionThrowsWhenRouteIsNull()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        WebApplication app = builder.Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => app.MapUxProjection<TestProjection>(null!));
        await app.DisposeAsync();
    }

    /// <summary>
    ///     Verifies that MapUxProjection with custom prefix registers endpoints with correct route pattern.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("MapUxProjection")]
    public async Task MapUxProjectionWithCustomPrefixRegistersEndpointsWithCorrectRoute()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        builder.Services.AddSingleton(factoryMock.Object);
        builder.Services.AddSingleton<ILogger<TestProjection>>(NullLogger<TestProjection>.Instance);
        WebApplication app = builder.Build();
        const string customPrefix = "custom/api";

        // Act
        RouteGroupBuilder group = app.MapUxProjection<TestProjection>(customPrefix, TestProjectionRoute);

        // Assert
        Assert.NotNull(group);
        await app.DisposeAsync();
    }

    /// <summary>
    ///     Verifies that MapUxProjection with route prefix throws when endpoints is null.
    /// </summary>
    [Fact]
    [AllureFeature("MapUxProjection")]
    public void MapUxProjectionWithPrefixThrowsWhenEndpointsIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            UxProjectionEndpointExtensions.MapUxProjection<TestProjection>(
                null!,
                TestRoutePrefix,
                TestProjectionRoute));
    }

    /// <summary>
    ///     Verifies that MapUxProjection with route prefix throws when route is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("MapUxProjection")]
    public async Task MapUxProjectionWithPrefixThrowsWhenRouteIsNull()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        WebApplication app = builder.Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => app.MapUxProjection<TestProjection>(TestRoutePrefix, null!));
        await app.DisposeAsync();
    }

    /// <summary>
    ///     Verifies that MapUxProjection with route prefix throws when route prefix is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("MapUxProjection")]
    public async Task MapUxProjectionWithPrefixThrowsWhenRoutePrefixIsNull()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        WebApplication app = builder.Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => app.MapUxProjection<TestProjection>(null!, TestProjectionRoute));
        await app.DisposeAsync();
    }

    /// <summary>
    ///     Verifies that MapUxProjections scans and registers projections from provided assembly.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("MapUxProjections")]
    public async Task MapUxProjectionsScansAndRegistersProjections()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        builder.Services.AddSingleton(factoryMock.Object);
        WebApplication app = builder.Build();
        Assembly testAssembly = Assembly.GetExecutingAssembly();

        // Act
        IEndpointRouteBuilder result = app.MapUxProjections(testAssembly);

        // Assert
        Assert.NotNull(result);
        Assert.Same(app, result);
        await app.DisposeAsync();
    }

    /// <summary>
    ///     Verifies that MapUxProjections skips types without both required attributes.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("MapUxProjections")]
    public async Task MapUxProjectionsSkipsTypesWithoutBothAttributes()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        builder.Services.AddSingleton(factoryMock.Object);
        WebApplication app = builder.Build();
        Assembly testAssembly = typeof(ProjectionWithoutUxAttribute).Assembly;

        // Act - Should not throw even though some types lack attributes
        IEndpointRouteBuilder result = app.MapUxProjections(testAssembly);

        // Assert
        Assert.NotNull(result);
        await app.DisposeAsync();
    }

    /// <summary>
    ///     Verifies that MapUxProjections throws when assemblies is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("MapUxProjections")]
    public async Task MapUxProjectionsThrowsWhenAssembliesIsNull()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        WebApplication app = builder.Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => app.MapUxProjections((Assembly[])null!));
        await app.DisposeAsync();
    }

    /// <summary>
    ///     Verifies that MapUxProjections throws when endpoints is null.
    /// </summary>
    [Fact]
    [AllureFeature("MapUxProjections")]
    public void MapUxProjectionsThrowsWhenEndpointsIsNull()
    {
        // Arrange
        Assembly testAssembly = Assembly.GetExecutingAssembly();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            UxProjectionEndpointExtensions.MapUxProjections(null!, testAssembly));
    }

    /// <summary>
    ///     Verifies that MapUxProjections uses entry assembly when no assemblies provided.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("MapUxProjections")]
    public async Task MapUxProjectionsUsesEntryAssemblyWhenNoAssembliesProvided()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        builder.Services.AddSingleton(factoryMock.Object);
        WebApplication app = builder.Build();

        // Act
        IEndpointRouteBuilder result = app.MapUxProjections();

        // Assert
        Assert.NotNull(result);
        Assert.Same(app, result);
        await app.DisposeAsync();
    }

    /// <summary>
    ///     Verifies that MapUxProjections with custom prefix scans and registers projections from provided assembly.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("MapUxProjections")]
    public async Task MapUxProjectionsWithCustomPrefixScansAndRegistersProjections()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        builder.Services.AddSingleton(factoryMock.Object);
        WebApplication app = builder.Build();
        Assembly testAssembly = Assembly.GetExecutingAssembly();
        const string customPrefix = "custom/api";

        // Act
        IEndpointRouteBuilder result = app.MapUxProjections(customPrefix, testAssembly);

        // Assert
        Assert.NotNull(result);
        Assert.Same(app, result);
        await app.DisposeAsync();
    }

    /// <summary>
    ///     Verifies that MapUxProjections with route prefix throws when assemblies is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("MapUxProjections")]
    public async Task MapUxProjectionsWithPrefixThrowsWhenAssembliesIsNull()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        WebApplication app = builder.Build();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => app.MapUxProjections(TestRoutePrefix, null!));
        await app.DisposeAsync();
    }

    /// <summary>
    ///     Verifies that MapUxProjections with route prefix throws when endpoints is null.
    /// </summary>
    [Fact]
    [AllureFeature("MapUxProjections")]
    public void MapUxProjectionsWithPrefixThrowsWhenEndpointsIsNull()
    {
        // Arrange
        Assembly testAssembly = Assembly.GetExecutingAssembly();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            UxProjectionEndpointExtensions.MapUxProjections(null!, TestRoutePrefix, testAssembly));
    }

    /// <summary>
    ///     Verifies that MapUxProjections with route prefix throws when route prefix is null.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    [AllureFeature("MapUxProjections")]
    public async Task MapUxProjectionsWithPrefixThrowsWhenRoutePrefixIsNull()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        WebApplication app = builder.Build();
        Assembly testAssembly = Assembly.GetExecutingAssembly();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => app.MapUxProjections((string)null!, testAssembly));
        await app.DisposeAsync();
    }
}