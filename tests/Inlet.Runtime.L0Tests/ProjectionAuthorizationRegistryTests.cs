using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Runtime.Abstractions;


namespace Mississippi.Inlet.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="IProjectionAuthorizationRegistry" />.
/// </summary>
public sealed class ProjectionAuthorizationRegistryTests : IDisposable
{
    private readonly IProjectionAuthorizationRegistry registry;

    private readonly ServiceProvider serviceProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectionAuthorizationRegistryTests" /> class.
    /// </summary>
    public ProjectionAuthorizationRegistryTests()
    {
        ServiceCollection services = [];
        services.AddInletSilo();
        serviceProvider = services.BuildServiceProvider();
        registry = serviceProvider.GetRequiredService<IProjectionAuthorizationRegistry>();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        serviceProvider.Dispose();
    }

    /// <summary>
    ///     GetAllPaths should return empty for a new registry.
    /// </summary>
    [Fact]
    public void GetAllPathsReturnsEmptyForNewRegistry()
    {
        // Act
        string[] paths = registry.GetAllPaths().ToArray();

        // Assert
        Assert.Empty(paths);
    }

    /// <summary>
    ///     GetAuthorizationMetadata should return null for unknown path.
    /// </summary>
    [Fact]
    public void GetAuthorizationMetadataReturnsNullForUnknownPath()
    {
        // Act
        ProjectionAuthorizationMetadata? metadata = registry.GetAuthorizationMetadata("/api/unknown");

        // Assert
        Assert.Null(metadata);
    }

    /// <summary>
    ///     GetAuthorizationMetadata should throw when path is null.
    /// </summary>
    [Fact]
    public void GetAuthorizationMetadataThrowsWhenPathNull()
    {
        // Act & Assert
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => registry.GetAuthorizationMetadata(null!));
        Assert.Equal("path", exception.ParamName);
    }

    /// <summary>
    ///     Register should overwrite existing metadata for a path.
    /// </summary>
    [Fact]
    public void RegisterOverwritesExistingMetadata()
    {
        // Arrange
        registry.Register("/api/accounts", new("first", null, null, true, false));
        ProjectionAuthorizationMetadata replacement = new("second", "admin", "Bearer", true, false);

        // Act
        registry.Register("/api/accounts", replacement);
        ProjectionAuthorizationMetadata? actual = registry.GetAuthorizationMetadata("/api/accounts");

        // Assert
        Assert.Equal(replacement, actual);
    }

    /// <summary>
    ///     Register should store and return metadata for the path.
    /// </summary>
    [Fact]
    public void RegisterStoresMetadata()
    {
        // Arrange
        ProjectionAuthorizationMetadata expected = new("projection.read", "reader", "Bearer", true, false);

        // Act
        registry.Register("/api/accounts", expected);
        ProjectionAuthorizationMetadata? actual = registry.GetAuthorizationMetadata("/api/accounts");

        // Assert
        Assert.Equal(expected, actual);
    }

    /// <summary>
    ///     Register should throw when metadata is null.
    /// </summary>
    [Fact]
    public void RegisterThrowsWhenMetadataNull()
    {
        // Act & Assert
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => registry.Register("/api/accounts", null!));
        Assert.Equal("metadata", exception.ParamName);
    }

    /// <summary>
    ///     Register should throw when path is null.
    /// </summary>
    [Fact]
    public void RegisterThrowsWhenPathNull()
    {
        // Act & Assert
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => registry.Register(null!, new(null, null, null, false, false)));
        Assert.Equal("path", exception.ParamName);
    }
}