using System;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Refraction.Abstractions.Theme;
using Mississippi.Refraction.Client.Infrastructure;


namespace Mississippi.Refraction.Client.L0Tests.Infrastructure;

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
    ///     AddRefraction rejects a null service collection.
    /// </summary>
    [Fact]
    public void AddRefractionRejectsNullServiceCollection()
    {
        // Act and assert
        Assert.Throws<ArgumentNullException>(() => RefractionServiceCollectionExtensions.AddRefraction(null!));
    }

    /// <summary>
    ///     AddRefraction preserves a host-registered theme catalog.
    /// </summary>
    [Fact]
    public void AddRefractionPreservesHostThemeCatalog()
    {
        // Arrange
        ServiceCollection services = new();
        TestRefractionThemeCatalog catalog = new();
        services.AddSingleton<IRefractionThemeCatalog>(catalog);

        // Act
        services.AddRefraction();
        using ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        Assert.Same(catalog, provider.GetRequiredService<IRefractionThemeCatalog>());
    }

    /// <summary>
    ///     AddRefraction registers the default Refraction theme catalog.
    /// </summary>
    [Fact]
    public void AddRefractionRegistersDefaultThemeCatalog()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddRefraction();
        using ServiceProvider provider = services.BuildServiceProvider();
        IRefractionThemeCatalog catalog = provider.GetRequiredService<IRefractionThemeCatalog>();

        // Assert
        Assert.Equal("horizon", catalog.DefaultTheme.BrandId.Value);
        Assert.Collection(
            catalog.Themes,
            theme => Assert.True(theme.IsDefault),
            theme => Assert.False(theme.IsDefault));
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