using System;
using System.Linq;
using System.Reflection;

using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Client.Components.Molecules;


namespace Mississippi.Refraction.Client.L0Tests.Components.Molecules;

/// <summary>
///     Tests for <see cref="ActionBar" /> component.
/// </summary>
public sealed class ActionBarTests : BunitContext
{
    /// <summary>
    ///     ActionBar has AdditionalAttributes parameter.
    /// </summary>
    [Fact]
    public void ActionBarHasAdditionalAttributesParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(ActionBar).GetProperty("AdditionalAttributes");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.True(attr!.CaptureUnmatchedValues);
    }

    /// <summary>
    ///     ActionBar has ChildContent parameter.
    /// </summary>
    [Fact]
    public void ActionBarHasChildContentParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(ActionBar).GetProperty("ChildContent");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(RenderFragment), prop.PropertyType);
    }

    /// <summary>
    ///     ActionBar exposes only the bounded presentational parameter surface.
    /// </summary>
    [Fact]
    public void ActionBarExposesOnlyBoundedPresentationalParameters()
    {
        // Arrange
        string[] parameterNames = typeof(ActionBar)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.GetCustomAttribute<ParameterAttribute>() is not null)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        // Assert
        Assert.Equal(["AdditionalAttributes", "ChildContent"], parameterNames);
    }

    /// <summary>
    ///     ActionBar inherits from ComponentBase.
    /// </summary>
    [Fact]
    public void ActionBarInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(ActionBar)));
    }

    /// <summary>
    ///     ActionBar merges an additional CSS class with the component root class.
    /// </summary>
    [Fact]
    public void ActionBarMergesAdditionalCssClassWithRootClass()
    {
        // Act
        using IRenderedComponent<ActionBar> cut = Render<ActionBar>(p => p.AddUnmatched("class", "action-host"));

        // Assert
        Assert.NotNull(cut.Find(".rf-action-bar.action-host"));
    }

    /// <summary>
    ///     ActionBar renders additional attributes.
    /// </summary>
    [Fact]
    public void ActionBarRendersAdditionalAttributes()
    {
        // Act
        using IRenderedComponent<ActionBar> cut = Render<ActionBar>(p => p.AddUnmatched("data-testid", "action-bar-1"));

        // Assert
        Assert.Equal("action-bar-1", cut.Find(".rf-action-bar").GetAttribute("data-testid"));
    }

    /// <summary>
    ///     ActionBar renders child content.
    /// </summary>
    [Fact]
    public void ActionBarRendersChildContent()
    {
        // Act
        using IRenderedComponent<ActionBar> cut = Render<ActionBar>(p => p.AddChildContent(
            "<span class='test-action'>Action 1</span>"));

        // Assert
        Assert.NotEmpty(cut.FindAll(".test-action"));
    }
}