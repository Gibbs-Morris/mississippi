using System.Reflection;

using Allure.Xunit.Attributes;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Atoms;


namespace Mississippi.Refraction.L0Tests.Components.Atoms;

/// <summary>
///     Tests for <see cref="ProgressArc" /> component.
/// </summary>
[AllureSuite("Refraction")]
[AllureSubSuite("Atoms")]
public sealed class ProgressArcTests
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
}