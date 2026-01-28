using System.Reflection;

using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Atoms;


namespace Mississippi.Refraction.L0Tests.Components.Atoms;

/// <summary>
///     Tests for <see cref="ProgressArc" /> component.
/// </summary>
public sealed class ProgressArcTests : BunitContext
{
    /// <summary>
    ///     ProgressArc has AdditionalAttributes parameter.
    /// </summary>
    [Fact]
    public void ProgressArcHasAdditionalAttributesParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(ProgressArc).GetProperty("AdditionalAttributes");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.True(attr!.CaptureUnmatchedValues);
    }

    /// <summary>
    ///     ProgressArc has Max parameter with default 100.
    /// </summary>
    [Fact]
    public void ProgressArcHasMaxParameterWithDefault100()
    {
        // Arrange
        PropertyInfo? prop = typeof(ProgressArc).GetProperty("Max");
        ProgressArc component = new();

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(100d, component.Max);
    }

    /// <summary>
    ///     ProgressArc has Min parameter with default 0.
    /// </summary>
    [Fact]
    public void ProgressArcHasMinParameterWithDefault0()
    {
        // Arrange
        PropertyInfo? prop = typeof(ProgressArc).GetProperty("Min");
        ProgressArc component = new();

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(0d, component.Min);
    }

    /// <summary>
    ///     ProgressArc has State parameter.
    /// </summary>
    [Fact]
    public void ProgressArcHasStateParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(ProgressArc).GetProperty("State");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     ProgressArc has Value parameter.
    /// </summary>
    [Fact]
    public void ProgressArcHasValueParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(ProgressArc).GetProperty("Value");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(double), prop!.PropertyType);
    }

    /// <summary>
    ///     ProgressArc inherits from ComponentBase.
    /// </summary>
    [Fact]
    public void ProgressArcInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(ProgressArc)));
    }

    /// <summary>
    ///     ProgressArc renders additional attributes.
    /// </summary>
    [Fact]
    public void ProgressArcRendersAdditionalAttributes()
    {
        // Act
        using IRenderedComponent<ProgressArc> cut =
            Render<ProgressArc>(p => p.AddUnmatched("data-testid", "progress-1"));

        // Assert
        Assert.Equal("progress-1", cut.Find(".rf-progress-arc").GetAttribute("data-testid"));
    }

    /// <summary>
    ///     ProgressArc renders aria-valuemax attribute.
    /// </summary>
    [Fact]
    public void ProgressArcRendersAriaValuemaxAttribute()
    {
        // Act
        using IRenderedComponent<ProgressArc> cut = Render<ProgressArc>(p => p.Add(c => c.Max, 200));

        // Assert
        string? ariaValuemax = cut.Find(".rf-progress-arc").GetAttribute("aria-valuemax");
        Assert.Equal("200", ariaValuemax);
    }

    /// <summary>
    ///     ProgressArc renders aria-valuemin attribute.
    /// </summary>
    [Fact]
    public void ProgressArcRendersAriaValueminAttribute()
    {
        // Act
        using IRenderedComponent<ProgressArc> cut = Render<ProgressArc>(p => p.Add(c => c.Min, 10));

        // Assert
        string? ariaValuemin = cut.Find(".rf-progress-arc").GetAttribute("aria-valuemin");
        Assert.Equal("10", ariaValuemin);
    }

    /// <summary>
    ///     ProgressArc renders aria-valuenow attribute.
    /// </summary>
    [Fact]
    public void ProgressArcRendersAriaValuenowAttribute()
    {
        // Act
        using IRenderedComponent<ProgressArc> cut = Render<ProgressArc>(p => p.Add(c => c.Value, 50));

        // Assert
        string? ariaValuenow = cut.Find(".rf-progress-arc").GetAttribute("aria-valuenow");
        Assert.Equal("50", ariaValuenow);
    }

    /// <summary>
    ///     ProgressArc renders custom state.
    /// </summary>
    [Fact]
    public void ProgressArcRendersCustomState()
    {
        // Act
        using IRenderedComponent<ProgressArc> cut = Render<ProgressArc>(p => p.Add(
            c => c.State,
            RefractionStates.Indeterminate));

        // Assert
        string? dataState = cut.Find(".rf-progress-arc").GetAttribute("data-state");
        Assert.Equal("indeterminate", dataState);
    }

    /// <summary>
    ///     ProgressArc renders SVG element.
    /// </summary>
    [Fact]
    public void ProgressArcRendersSvgElement()
    {
        // Act
        using IRenderedComponent<ProgressArc> cut = Render<ProgressArc>();

        // Assert
        Assert.NotEmpty(cut.FindAll(".rf-progress-arc__svg"));
    }

    /// <summary>
    ///     ProgressArc renders with default state.
    /// </summary>
    [Fact]
    public void ProgressArcRendersWithDefaultState()
    {
        // Act
        using IRenderedComponent<ProgressArc> cut = Render<ProgressArc>();

        // Assert
        string? dataState = cut.Find(".rf-progress-arc").GetAttribute("data-state");
        Assert.Equal("determinate", dataState);
    }

    /// <summary>
    ///     ProgressArc renders with progressbar role for accessibility.
    /// </summary>
    [Fact]
    public void ProgressArcRendersWithProgressbarRoleForAccessibility()
    {
        // Act
        using IRenderedComponent<ProgressArc> cut = Render<ProgressArc>();

        // Assert
        string? role = cut.Find(".rf-progress-arc").GetAttribute("role");
        Assert.Equal("progressbar", role);
    }

    /// <summary>
    ///     ProgressArc State defaults to Determinate.
    /// </summary>
    [Fact]
    public void ProgressArcStateDefaultsToDeterminate()
    {
        // Arrange
        ProgressArc component = new();

        // Assert
        Assert.Equal(RefractionStates.Determinate, component.State);
    }

    /// <summary>
    ///     ProgressArc Value defaults to 0.
    /// </summary>
    [Fact]
    public void ProgressArcValueDefaultsToZero()
    {
        // Arrange
        ProgressArc component = new();

        // Assert
        Assert.Equal(0d, component.Value);
    }
}