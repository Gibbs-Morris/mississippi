using System;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Refraction.Client.Infrastructure;


namespace Mississippi.Refraction.Client.L0Tests.Infrastructure;

/// <summary>
///     Tests for <see cref="RefractionServiceCollectionExtensions" />.
/// </summary>
public sealed class RefractionServiceCollectionExtensionsTests
{
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

    /// <summary>
    ///     UseRefraction is static extension method.
    /// </summary>
    [Fact]
    public void UseRefractionIsStaticExtensionMethod()
    {
        // Arrange
        MethodInfo? method = typeof(RefractionServiceCollectionExtensions).GetMethod("UseRefraction");

        // Assert
        Assert.NotNull(method);
        Assert.True(method!.IsStatic);
    }

    /// <summary>
    ///     UseRefraction returns service collection for chaining.
    /// </summary>
    [Fact]
    public void UseRefractionReturnsServiceCollectionForChaining()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.UseRefraction();

        // Assert
        Assert.Same(services, result);
    }
}