using System.Reflection;

using Allure.Xunit.Attributes;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Atoms;


namespace Mississippi.Refraction.L0Tests.Components.Atoms;

/// <summary>
///     Tests for <see cref="CalloutLine" /> component.
/// </summary>
[AllureSuite("Refraction")]
[AllureSubSuite("Atoms")]
public sealed class CalloutLineTests
{
    /// <summary>
    ///     CalloutLine has AdditionalAttributes parameter with CaptureUnmatchedValues.
    /// </summary>
    [Fact]
    [AllureFeature("CalloutLine")]
    public void CalloutLineHasAdditionalAttributesParameterWithCaptureUnmatchedValues()
    {
        // Arrange
        PropertyInfo? prop = typeof(CalloutLine).GetProperty("AdditionalAttributes");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.True(attr!.CaptureUnmatchedValues);
    }

    /// <summary>
    ///     CalloutLine has Label parameter.
    /// </summary>
    [Fact]
    [AllureFeature("CalloutLine")]
    public void CalloutLineHasLabelParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(CalloutLine).GetProperty("Label");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(string), prop!.PropertyType);
    }

    /// <summary>
    ///     CalloutLine has State parameter.
    /// </summary>
    [Fact]
    [AllureFeature("CalloutLine")]
    public void CalloutLineHasStateParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(CalloutLine).GetProperty("State");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(string), prop!.PropertyType);
    }

    /// <summary>
    ///     CalloutLine inherits from ComponentBase.
    /// </summary>
    [Fact]
    [AllureFeature("CalloutLine")]
    public void CalloutLineInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(CalloutLine)));
    }

    /// <summary>
    ///     CalloutLine State defaults to Idle.
    /// </summary>
    [Fact]
    [AllureFeature("CalloutLine")]
    public void CalloutLineStateDefaultsToIdle()
    {
        // Arrange
        CalloutLine component = new();

        // Assert
        Assert.Equal(RefractionStates.Idle, component.State);
    }
}