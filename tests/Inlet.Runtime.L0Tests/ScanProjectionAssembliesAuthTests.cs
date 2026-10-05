using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Runtime.Abstractions;
using Mississippi.Inlet.Runtime.L0Tests.Infrastructure;


namespace Mississippi.Inlet.Runtime.L0Tests;

/// <summary>
///     Tests authorization metadata population during projection assembly scanning.
/// </summary>
public sealed class ScanProjectionAssembliesAuthTests
{
    /// <summary>
    ///     ScanProjectionAssemblies should not register authorization metadata for projection without auth attributes.
    /// </summary>
    [Fact]
    public void ScanProjectionAssembliesDoesNotRegisterAuthorizationMetadataWhenNoAuthAttributesPresent()
    {
        // Arrange
        ServiceCollection services = [];
        Assembly testAssembly = typeof(PathOnlyProjection).Assembly;

        // Act
        services.ScanProjectionAssemblies(testAssembly);
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectionAuthorizationRegistry registry = provider.GetRequiredService<IProjectionAuthorizationRegistry>();
        ProjectionAuthorizationMetadata? metadata = registry.GetAuthorizationMetadata("/api/path-only-projection");

        // Assert
        Assert.Null(metadata);
    }

    /// <summary>
    ///     ScanProjectionAssemblies should ignore authorization metadata when projection path is missing.
    /// </summary>
    [Fact]
    public void ScanProjectionAssembliesIgnoresAuthorizationMetadataWhenProjectionPathMissing()
    {
        // Arrange
        ServiceCollection services = [];
        Assembly testAssembly = typeof(AuthorizationWithoutPathType).Assembly;

        // Act
        services.ScanProjectionAssemblies(testAssembly);
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectionAuthorizationRegistry registry = provider.GetRequiredService<IProjectionAuthorizationRegistry>();

        // Assert
        ProjectionAuthorizationMetadata? metadata = registry.GetAuthorizationMetadata("/api/no-projection-path");
        Assert.Null(metadata);
    }

    /// <summary>
    ///     ScanProjectionAssemblies should register allow-anonymous metadata for decorated projections.
    /// </summary>
    [Fact]
    public void ScanProjectionAssembliesRegistersAllowAnonymousMetadataForDecoratedProjection()
    {
        // Arrange
        ServiceCollection services = [];
        Assembly testAssembly = typeof(AllowAnonymousProjection).Assembly;

        // Act
        services.ScanProjectionAssemblies(testAssembly);
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectionAuthorizationRegistry registry = provider.GetRequiredService<IProjectionAuthorizationRegistry>();
        ProjectionAuthorizationMetadata? metadata = registry.GetAuthorizationMetadata("/api/anonymous-projection");

        // Assert
        Assert.NotNull(metadata);
        Assert.Null(metadata.Policy);
        Assert.Null(metadata.Roles);
        Assert.Null(metadata.AuthenticationSchemes);
        Assert.False(metadata.HasAuthorize);
        Assert.True(metadata.HasAllowAnonymous);
    }

    /// <summary>
    ///     ScanProjectionAssemblies should register authorization metadata for decorated projections.
    /// </summary>
    [Fact]
    public void ScanProjectionAssembliesRegistersAuthorizationMetadataForDecoratedProjection()
    {
        // Arrange
        ServiceCollection services = [];
        Assembly testAssembly = typeof(AuthorizedProjection).Assembly;

        // Act
        services.ScanProjectionAssemblies(testAssembly);
        using ServiceProvider provider = services.BuildServiceProvider();
        IProjectionAuthorizationRegistry registry = provider.GetRequiredService<IProjectionAuthorizationRegistry>();
        ProjectionAuthorizationMetadata? metadata = registry.GetAuthorizationMetadata("/api/authorized-projection");

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("runtime.tests.policy", metadata.Policy);
        Assert.Equal("reader", metadata.Roles);
        Assert.Equal("Bearer", metadata.AuthenticationSchemes);
        Assert.True(metadata.HasAuthorize);
        Assert.False(metadata.HasAllowAnonymous);
    }
}