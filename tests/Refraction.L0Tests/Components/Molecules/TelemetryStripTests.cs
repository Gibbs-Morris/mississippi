using System.Reflection;

using Allure.Xunit.Attributes;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Molecules;


namespace Mississippi.Refraction.L0Tests.Components.Molecules;

/// <summary>
///     Tests for <see cref="TelemetryStrip" /> component.
/// </summary>
[AllureSuite("Refraction")]
[AllureSubSuite("Molecules")]
public sealed class TelemetryStripTests
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