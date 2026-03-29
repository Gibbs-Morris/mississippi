using System.Reflection;

using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Client.Components.Molecules;


namespace Mississippi.Refraction.Client.L0Tests.Components.Molecules;

/// <summary>
///     Tests for <see cref="StatusSummaryBar" /> component.
/// </summary>
public sealed class StatusSummaryBarTests : BunitContext
{
    /// <summary>
    ///     StatusSummaryBar has AdditionalAttributes parameter.
    /// </summary>
    [Fact]
    public void StatusSummaryBarHasAdditionalAttributesParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(StatusSummaryBar).GetProperty("AdditionalAttributes");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.True(attr!.CaptureUnmatchedValues);
    }

    /// <summary>
    ///     StatusSummaryBar has ChildContent parameter.
    /// </summary>
    [Fact]
    public void StatusSummaryBarHasChildContentParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(StatusSummaryBar).GetProperty("ChildContent");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(RenderFragment), prop.PropertyType);
    }

    /// <summary>
    ///     StatusSummaryBar has State parameter.
    /// </summary>
    [Fact]
    public void StatusSummaryBarHasStateParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(StatusSummaryBar).GetProperty("State");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     StatusSummaryBar inherits from ComponentBase.
    /// </summary>
    [Fact]
    public void StatusSummaryBarInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(StatusSummaryBar)));
    }

    /// <summary>
    ///     StatusSummaryBar renders additional attributes.
    /// </summary>
    [Fact]
    public void StatusSummaryBarRendersAdditionalAttributes()
    {
        // Act
        using IRenderedComponent<StatusSummaryBar> cut =
            Render<StatusSummaryBar>(p => p.AddUnmatched("data-testid", "status-summary-1"));

        // Assert
        Assert.Equal("status-summary-1", cut.Find(".rf-status-summary-bar").GetAttribute("data-testid"));
    }

    /// <summary>
    ///     StatusSummaryBar merges an additional CSS class with the component root class.
    /// </summary>
    [Fact]
    public void StatusSummaryBarMergesAdditionalCssClassWithRootClass()
    {
        // Act
        using IRenderedComponent<StatusSummaryBar> cut =
            Render<StatusSummaryBar>(p => p.AddUnmatched("class", "summary-host"));

        // Assert
        Assert.NotNull(cut.Find(".rf-status-summary-bar.summary-host"));
    }

    /// <summary>
    ///     StatusSummaryBar renders child content.
    /// </summary>
    [Fact]
    public void StatusSummaryBarRendersChildContent()
    {
        // Act
        using IRenderedComponent<StatusSummaryBar> cut = Render<StatusSummaryBar>(p => p.AddChildContent(
            "<span class='test-summary-item'>Summary Item</span>"));

        // Assert
        Assert.NotEmpty(cut.FindAll(".test-summary-item"));
    }

    /// <summary>
    ///     StatusSummaryBar renders the status role.
    /// </summary>
    [Fact]
    public void StatusSummaryBarRendersStatusRole()
    {
        // Act
        using IRenderedComponent<StatusSummaryBar> cut = Render<StatusSummaryBar>();

        // Assert
        Assert.Equal("status", cut.Find(".rf-status-summary-bar").GetAttribute("role"));
    }

    /// <summary>
    ///     StatusSummaryBar renders custom state.
    /// </summary>
    [Fact]
    public void StatusSummaryBarRendersCustomState()
    {
        // Act
        using IRenderedComponent<StatusSummaryBar> cut = Render<StatusSummaryBar>(p => p.Add(
            c => c.State,
            RefractionStates.Active));

        // Assert
        Assert.Equal("active", cut.Find(".rf-status-summary-bar").GetAttribute("data-state"));
    }

    /// <summary>
    ///     StatusSummaryBar renders with default state.
    /// </summary>
    [Fact]
    public void StatusSummaryBarRendersWithDefaultState()
    {
        // Act
        using IRenderedComponent<StatusSummaryBar> cut = Render<StatusSummaryBar>();

        // Assert
        Assert.Equal("quiet", cut.Find(".rf-status-summary-bar").GetAttribute("data-state"));
    }

    /// <summary>
    ///     StatusSummaryBar State defaults to Quiet.
    /// </summary>
    [Fact]
    public void StatusSummaryBarStateDefaultsToQuiet()
    {
        // Arrange
        StatusSummaryBar component = new();

        // Assert
        Assert.Equal(RefractionStates.Quiet, component.State);
    }
}