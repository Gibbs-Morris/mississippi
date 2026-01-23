using System.Reflection;

using Allure.Xunit.Attributes;

using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Atoms;


namespace Mississippi.Refraction.L0Tests.Components.Atoms;

/// <summary>
///     Tests for <see cref="ProgressArc" /> component.
/// </summary>
[AllureSuite("Refraction")]
[AllureSubSuite("Atoms")]
public sealed class ProgressArcTests : BunitContext
{
    /// <summary>
    ///     ProgressArc has AdditionalAttributes parameter.
    /// </summary>
    [Fact]
    [AllureFeature("ProgressArc")]
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
    [AllureFeature("ProgressArc")]
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
    [AllureFeature("ProgressArc")]
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
    [AllureFeature("ProgressArc")]
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
    [AllureFeature("ProgressArc")]
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
    [AllureFeature("ProgressArc")]
    public void ProgressArcInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(ProgressArc)));
    }

    /// <summary>
    ///     ProgressArc State defaults to Determinate.
    /// </summary>
    [Fact]
    [AllureFeature("ProgressArc")]
    public void ProgressArcStateDefaultsToDeterminate()
    {
        // Arrange
        ProgressArc component = new();

        // Assert
        Assert.Equal(RefractionStates.Determinate, component.State);
    }

    /// <summary>
    ///     ProgressArc renders with default state.
    /// </summary>
    [Fact]
    [AllureFeature("ProgressArc")]
    public void ProgressArcRendersWithDefaultState()
    {
        // Act
        using var cut = Render<ProgressArc>();

        // Assert
        string? dataState = cut.Find(".rf-progress-arc").GetAttribute("data-state");
        Assert.Equal("determinate", dataState);
    }

    /// <summary>
    ///     ProgressArc renders custom state.
    /// </summary>
    [Fact]
    [AllureFeature("ProgressArc")]
    public void ProgressArcRendersCustomState()
    {
        // Act
        using var cut = Render<ProgressArc>(p => p
            .Add(c => c.State, RefractionStates.Indeterminate));

        // Assert
        string? dataState = cut.Find(".rf-progress-arc").GetAttribute("data-state");
        Assert.Equal("indeterminate", dataState);
    }

    /// <summary>
    ///     ProgressArc renders additional attributes.
    /// </summary>
    [Fact]
    [AllureFeature("ProgressArc")]
    public void ProgressArcRendersAdditionalAttributes()
    {
        // Act
        using var cut = Render<ProgressArc>(p => p
            .AddUnmatched("data-testid", "progress-1"));

        // Assert
        Assert.Equal("progress-1", cut.Find(".rf-progress-arc").GetAttribute("data-testid"));
    }

    /// <summary>
    ///     ProgressArc renders SVG element.
    /// </summary>
    [Fact]
    [AllureFeature("ProgressArc")]
    public void ProgressArcRendersSvgElement()
    {
        // Act
        using var cut = Render<ProgressArc>();

        // Assert
        Assert.NotEmpty(cut.FindAll(".rf-progress-arc__svg"));
    }
}