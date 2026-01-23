using System.Reflection;

using Allure.Xunit.Attributes;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Atoms;


namespace Mississippi.Refraction.L0Tests.Components.Atoms;

/// <summary>
///     Smoke tests for <see cref="Emitter" /> component.
/// </summary>
[AllureSuite("Refraction")]
[AllureSubSuite("Atoms")]
public sealed class EmitterTests
{
    /// <summary>
    ///     Emitter has AdditionalAttributes parameter with CaptureUnmatchedValues.
    /// </summary>
    [Fact]
    [AllureFeature("Emitter")]
    public void EmitterHasAdditionalAttributesParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(Emitter).GetProperty("AdditionalAttributes");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.True(attr!.CaptureUnmatchedValues);
    }

    /// <summary>
    ///     Emitter has OnActivate EventCallback.
    /// </summary>
    [Fact]
    [AllureFeature("Emitter")]
    public void EmitterHasOnActivateEventCallback()
    {
        // Arrange
        PropertyInfo? prop = typeof(Emitter).GetProperty("OnActivate");

        // Assert
        Assert.NotNull(prop);
        Assert.True(prop!.PropertyType.IsGenericType);
        ParameterAttribute? attr = prop.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     Emitter has OnFocus EventCallback.
    /// </summary>
    [Fact]
    [AllureFeature("Emitter")]
    public void EmitterHasOnFocusEventCallback()
    {
        // Arrange
        PropertyInfo? prop = typeof(Emitter).GetProperty("OnFocus");

        // Assert
        Assert.NotNull(prop);
        Assert.True(prop!.PropertyType.IsGenericType);
        ParameterAttribute? attr = prop.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     Emitter has State parameter.
    /// </summary>
    [Fact]
    [AllureFeature("Emitter")]
    public void EmitterHasStateParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(Emitter).GetProperty("State");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     Emitter inherits from ComponentBase.
    /// </summary>
    [Fact]
    [AllureFeature("Emitter")]
    public void EmitterInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(Emitter)));
    }

    /// <summary>
    ///     Emitter State defaults to Idle.
    /// </summary>
    [Fact]
    [AllureFeature("Emitter")]
    public void EmitterStateDefaultsToIdle()
    {
        // Arrange
        Emitter emitter = new();

        // Assert
        Assert.Equal(RefractionStates.Idle, emitter.State);
    }
}