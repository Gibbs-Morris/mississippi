using System;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Refraction.Infrastructure;


namespace Mississippi.Refraction.L0Tests.Infrastructure;

/// <summary>
///     Tests for <see cref="RefractionServiceCollectionExtensions" />.
/// </summary>
public sealed class RefractionServiceCollectionExtensionsTests
{
    /// <summary>
    ///     AddRefraction is static extension method.
    /// </summary>
    [Fact]
    public void AddRefractionIsStaticExtensionMethod()
    {
        // Arrange
        MethodInfo? method = typeof(RefractionServiceCollectionExtensions).GetMethod("AddRefraction");

        // Assert
        Assert.NotNull(method);
        Assert.True(method!.IsStatic);
    }

    /// <summary>
    ///     AddRefraction returns service collection for chaining.
    /// </summary>
    [Fact]
    public void AddRefractionReturnsServiceCollectionForChaining()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddRefraction();

        // Assert
        Assert.Same(services, result);
    }

    /// <summary>
    ///     RefractionServiceCollectionExtensions class is static.
    /// </summary>
    [Fact]
    public void RefractionServiceCollectionExtensionsClassIsStatic()
    {
        // Arrange
        Type extensionsType = typeof(RefractionServiceCollectionExtensions);

        // Assert
        Assert.True(extensionsType.IsAbstract);
        Assert.True(extensionsType.IsSealed);
    }
}