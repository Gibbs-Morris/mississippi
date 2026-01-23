using System.Reflection;

using Allure.Xunit.Attributes;

using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Molecules;


namespace Mississippi.Refraction.L0Tests.Components.Molecules;

/// <summary>
///     Tests for <see cref="TelemetryStrip" /> component.
/// </summary>
[AllureSuite("Refraction")]
[AllureSubSuite("Molecules")]
public sealed class TelemetryStripTests : BunitContext
{
    /// <summary>
    ///     TelemetryStrip has AdditionalAttributes parameter.
    /// </summary>
    [Fact]
    [AllureFeature("TelemetryStrip")]
    public void TelemetryStripHasAdditionalAttributesParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(TelemetryStrip).GetProperty("AdditionalAttributes");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.True(attr!.CaptureUnmatchedValues);
    }

    /// <summary>
    ///     TelemetryStrip has ChildContent parameter.
    /// </summary>
    [Fact]
    [AllureFeature("TelemetryStrip")]
    public void TelemetryStripHasChildContentParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(TelemetryStrip).GetProperty("ChildContent");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(RenderFragment), prop!.PropertyType);
    }

    /// <summary>
    ///     TelemetryStrip has State parameter.
    /// </summary>
    [Fact]
    [AllureFeature("TelemetryStrip")]
    public void TelemetryStripHasStateParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(TelemetryStrip).GetProperty("State");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     TelemetryStrip inherits from ComponentBase.
    /// </summary>
    [Fact]
    [AllureFeature("TelemetryStrip")]
    public void TelemetryStripInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(TelemetryStrip)));
    }

    /// <summary>
    ///     TelemetryStrip renders additional attributes.
    /// </summary>
    [Fact]
    [AllureFeature("TelemetryStrip")]
    public void TelemetryStripRendersAdditionalAttributes()
    {
        // Act
        using IRenderedComponent<TelemetryStrip> cut =
            Render<TelemetryStrip>(p => p.AddUnmatched("data-testid", "strip-1"));

        // Assert
        Assert.Equal("strip-1", cut.Find(".rf-telemetry-strip").GetAttribute("data-testid"));
    }

    /// <summary>
    ///     TelemetryStrip renders child content.
    /// </summary>
    [Fact]
    [AllureFeature("TelemetryStrip")]
    public void TelemetryStripRendersChildContent()
    {
        // Act
        using IRenderedComponent<TelemetryStrip> cut = Render<TelemetryStrip>(p => p.AddChildContent(
            "<span class='test-telemetry'>Telemetry Data</span>"));

        // Assert
        Assert.NotEmpty(cut.FindAll(".test-telemetry"));
    }

    /// <summary>
    ///     TelemetryStrip renders custom state.
    /// </summary>
    [Fact]
    [AllureFeature("TelemetryStrip")]
    public void TelemetryStripRendersCustomState()
    {
        // Act
        using IRenderedComponent<TelemetryStrip> cut = Render<TelemetryStrip>(p => p.Add(
            c => c.State,
            RefractionStates.Active));

        // Assert
        string? dataState = cut.Find(".rf-telemetry-strip").GetAttribute("data-state");
        Assert.Equal("active", dataState);
    }

    /// <summary>
    ///     TelemetryStrip renders with default state.
    /// </summary>
    [Fact]
    [AllureFeature("TelemetryStrip")]
    public void TelemetryStripRendersWithDefaultState()
    {
        // Act
        using IRenderedComponent<TelemetryStrip> cut = Render<TelemetryStrip>();

        // Assert
        string? dataState = cut.Find(".rf-telemetry-strip").GetAttribute("data-state");
        Assert.Equal("quiet", dataState);
    }

    /// <summary>
    ///     TelemetryStrip State defaults to Quiet.
    /// </summary>
    [Fact]
    [AllureFeature("TelemetryStrip")]
    public void TelemetryStripStateDefaultsToQuiet()
    {
        // Arrange
        TelemetryStrip component = new();

        // Assert
        Assert.Equal(RefractionStates.Quiet, component.State);
    }
}