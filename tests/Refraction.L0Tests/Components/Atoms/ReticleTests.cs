using System.Reflection;

using Allure.Xunit.Attributes;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Atoms;


namespace Mississippi.Refraction.L0Tests.Components.Atoms;

/// <summary>
///     Tests for <see cref="Reticle" /> component.
/// </summary>
[AllureSuite("Refraction")]
[AllureSubSuite("Atoms")]
public sealed class ReticleTests
{
    /// <summary>
    ///     Reticle has AdditionalAttributes parameter.
    /// </summary>
    [Fact]
    [AllureFeature("Reticle")]
    public void ReticleHasAdditionalAttributesParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(Reticle).GetProperty("AdditionalAttributes");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.True(attr!.CaptureUnmatchedValues);
    }

    /// <summary>
    ///     Reticle has Mode parameter.
    /// </summary>
    [Fact]
    [AllureFeature("Reticle")]
    public void ReticleHasModeParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(Reticle).GetProperty("Mode");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(string), prop!.PropertyType);
    }

    /// <summary>
    ///     Reticle has State parameter.
    /// </summary>
    [Fact]
    [AllureFeature("Reticle")]
    public void ReticleHasStateParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(Reticle).GetProperty("State");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     Reticle inherits from ComponentBase.
    /// </summary>
    [Fact]
    [AllureFeature("Reticle")]
    public void ReticleInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(Reticle)));
    }

    /// <summary>
    ///     Reticle Mode defaults to Focus.
    /// </summary>
    [Fact]
    [AllureFeature("Reticle")]
    public void ReticleModeDefaultsToFocus()
    {
        // Arrange
        Reticle component = new();

        // Assert
        Assert.Equal(RefractionReticleModes.Focus, component.Mode);
    }

    /// <summary>
    ///     Reticle State defaults to Idle.
    /// </summary>
    [Fact]
    [AllureFeature("Reticle")]
    public void ReticleStateDefaultsToIdle()
    {
        // Arrange
        Reticle component = new();

        // Assert
        Assert.Equal(RefractionStates.Idle, component.State);
    }
}