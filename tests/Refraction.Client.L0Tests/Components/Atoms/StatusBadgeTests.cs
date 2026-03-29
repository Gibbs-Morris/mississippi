using System;
using System.Reflection;

using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Client.Components.Atoms;


namespace Mississippi.Refraction.Client.L0Tests.Components.Atoms;

/// <summary>
///     Tests for <see cref="StatusBadge" /> component.
/// </summary>
public sealed class StatusBadgeTests : BunitContext
{
    /// <summary>
    ///     StatusBadge has AdditionalAttributes parameter with CaptureUnmatchedValues.
    /// </summary>
    [Fact]
    public void StatusBadgeHasAdditionalAttributesParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(StatusBadge).GetProperty("AdditionalAttributes");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.True(attr!.CaptureUnmatchedValues);
    }

    /// <summary>
    ///     StatusBadge has Label parameter.
    /// </summary>
    [Fact]
    public void StatusBadgeHasLabelParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(StatusBadge).GetProperty("Label");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(string), prop.PropertyType);
    }

    /// <summary>
    ///     StatusBadge has State parameter.
    /// </summary>
    [Fact]
    public void StatusBadgeHasStateParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(StatusBadge).GetProperty("State");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     StatusBadge inherits from ComponentBase.
    /// </summary>
    [Fact]
    public void StatusBadgeInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(StatusBadge)));
    }

    /// <summary>
    ///     StatusBadge merges an additional CSS class with the component root class.
    /// </summary>
    [Fact]
    public void StatusBadgeMergesAdditionalCssClassWithRootClass()
    {
        // Act
        using IRenderedComponent<StatusBadge> cut = Render<StatusBadge>(parameters => parameters
            .Add(component => component.Label, "Pending review")
            .AddUnmatched("class", "status-host"));

        // Assert
        Assert.NotNull(cut.Find(".rf-status-badge.status-host"));
    }

    /// <summary>
    ///     StatusBadge renders additional attributes.
    /// </summary>
    [Fact]
    public void StatusBadgeRendersAdditionalAttributes()
    {
        // Act
        using IRenderedComponent<StatusBadge> cut = Render<StatusBadge>(parameters => parameters
            .Add(component => component.Label, "Ready")
            .AddUnmatched("data-testid", "status-badge-1"));

        // Assert
        Assert.Equal("status-badge-1", cut.Find(".rf-status-badge").GetAttribute("data-testid"));
    }

    /// <summary>
    ///     StatusBadge renders custom state.
    /// </summary>
    [Fact]
    public void StatusBadgeRendersCustomState()
    {
        // Act
        using IRenderedComponent<StatusBadge> cut = Render<StatusBadge>(parameters => parameters
            .Add(component => component.Label, "Ready")
            .Add(component => component.State, RefractionStates.Active));

        // Assert
        Assert.Equal("active", cut.Find(".rf-status-badge").GetAttribute("data-state"));
    }

    /// <summary>
    ///     StatusBadge renders the label.
    /// </summary>
    [Fact]
    public void StatusBadgeRendersLabel()
    {
        // Act
        using IRenderedComponent<StatusBadge> cut = Render<StatusBadge>(parameters => parameters.Add(
            component => component.Label,
            "Actioned"));

        // Assert
        Assert.Contains("Actioned", cut.Find(".rf-status-badge").TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    ///     StatusBadge renders with default state.
    /// </summary>
    [Fact]
    public void StatusBadgeRendersWithDefaultState()
    {
        // Act
        using IRenderedComponent<StatusBadge> cut = Render<StatusBadge>(parameters => parameters.Add(
            component => component.Label,
            "Ready"));

        // Assert
        Assert.Equal("idle", cut.Find(".rf-status-badge").GetAttribute("data-state"));
    }

    /// <summary>
    ///     StatusBadge State defaults to Idle.
    /// </summary>
    [Fact]
    public void StatusBadgeStateDefaultsToIdle()
    {
        // Arrange
        StatusBadge component = new();

        // Assert
        Assert.Equal(RefractionStates.Idle, component.State);
    }
}