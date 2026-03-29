using System;
using System.Linq;
using System.Reflection;

using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Client.Components.Molecules;


namespace Mississippi.Refraction.Client.L0Tests.Components.Molecules;

/// <summary>
///     Tests for <see cref="FilterBar" /> component.
/// </summary>
public sealed class FilterBarTests : BunitContext
{
    /// <summary>
    ///     FilterBar exposes only the bounded presentational parameter surface.
    /// </summary>
    [Fact]
    public void FilterBarExposesOnlyBoundedPresentationalParameters()
    {
        // Arrange
        string[] parameterNames = typeof(FilterBar).GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.GetCustomAttribute<ParameterAttribute>() is not null)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        // Assert
        Assert.Equal(["AdditionalAttributes", "ChildContent"], parameterNames);
    }

    /// <summary>
    ///     FilterBar has AdditionalAttributes parameter.
    /// </summary>
    [Fact]
    public void FilterBarHasAdditionalAttributesParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(FilterBar).GetProperty("AdditionalAttributes");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.True(attr!.CaptureUnmatchedValues);
    }

    /// <summary>
    ///     FilterBar has ChildContent parameter.
    /// </summary>
    [Fact]
    public void FilterBarHasChildContentParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(FilterBar).GetProperty("ChildContent");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(RenderFragment), prop.PropertyType);
    }

    /// <summary>
    ///     FilterBar inherits from ComponentBase.
    /// </summary>
    [Fact]
    public void FilterBarInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(FilterBar)));
    }

    /// <summary>
    ///     FilterBar merges an additional CSS class with the component root class.
    /// </summary>
    [Fact]
    public void FilterBarMergesAdditionalCssClassWithRootClass()
    {
        // Act
        using IRenderedComponent<FilterBar> cut = Render<FilterBar>(p => p.AddUnmatched("class", "filter-host"));

        // Assert
        Assert.NotNull(cut.Find(".rf-filter-bar.filter-host"));
    }

    /// <summary>
    ///     FilterBar renders additional attributes.
    /// </summary>
    [Fact]
    public void FilterBarRendersAdditionalAttributes()
    {
        // Act
        using IRenderedComponent<FilterBar> cut = Render<FilterBar>(p => p.AddUnmatched("data-testid", "filter-bar-1"));

        // Assert
        Assert.Equal("filter-bar-1", cut.Find(".rf-filter-bar").GetAttribute("data-testid"));
    }

    /// <summary>
    ///     FilterBar renders child content.
    /// </summary>
    [Fact]
    public void FilterBarRendersChildContent()
    {
        // Act
        using IRenderedComponent<FilterBar> cut = Render<FilterBar>(p => p.AddChildContent(
            "<span class='test-filter'>Filter 1</span>"));

        // Assert
        Assert.NotEmpty(cut.FindAll(".test-filter"));
    }
}