using System;
using System.Reflection;


using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Infrastructure;


namespace Mississippi.Refraction.L0Tests.Infrastructure;

/// <summary>
///     Tests for <see cref="CascadingRefractionProvider" />.
/// </summary>
public sealed class CascadingRefractionProviderTests
{
    /// <summary>
    ///     CascadingRefractionProvider has ChildContent parameter.
    /// </summary>
    [Fact]
        public void CascadingRefractionProviderHasChildContentParameter()
    {
        // Arrange
        Type componentType = typeof(CascadingRefractionProvider);

        // Act
        PropertyInfo? childContent = componentType.GetProperty("ChildContent");

        // Assert
        Assert.NotNull(childContent);
        Assert.Equal(typeof(RenderFragment), childContent!.PropertyType);
        Assert.NotNull(childContent.GetCustomAttribute<ParameterAttribute>());
    }

    /// <summary>
    ///     CascadingRefractionProvider has ReducedMotion parameter.
    /// </summary>
    [Fact]
        public void CascadingRefractionProviderHasReducedMotionParameter()
    {
        // Arrange
        Type componentType = typeof(CascadingRefractionProvider);

        // Act
        PropertyInfo? reducedMotion = componentType.GetProperty("ReducedMotion");

        // Assert
        Assert.NotNull(reducedMotion);
        Assert.Equal(typeof(bool), reducedMotion!.PropertyType);
        Assert.NotNull(reducedMotion.GetCustomAttribute<ParameterAttribute>());
    }

    /// <summary>
    ///     CascadingRefractionProvider inherits from ComponentBase.
    /// </summary>
    [Fact]
        public void CascadingRefractionProviderInheritsFromComponentBase()
    {
        // Arrange
        Type componentType = typeof(CascadingRefractionProvider);

        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(componentType));
    }

    /// <summary>
    ///     CascadingRefractionProvider ReducedMotion defaults to false.
    /// </summary>
    [Fact]
        public void CascadingRefractionProviderReducedMotionDefaultsToFalse()
    {
        // Arrange
        CascadingRefractionProvider provider = new();

        // Act
        PropertyInfo? reducedMotion = typeof(CascadingRefractionProvider).GetProperty("ReducedMotion");

        // Assert
        Assert.NotNull(reducedMotion);
        object? value = reducedMotion!.GetValue(provider);
        Assert.False((bool)value!);
    }
}