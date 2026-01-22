using System;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.EventSourcing.UxProjections.Abstractions;

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
    ///     Test projection DTO for endpoint registration tests.
    /// </summary>
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
}