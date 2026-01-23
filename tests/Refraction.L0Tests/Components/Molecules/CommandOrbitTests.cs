using System.Reflection;

using Allure.Xunit.Attributes;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Molecules;


namespace Mississippi.Refraction.L0Tests.Components.Molecules;

/// <summary>
///     Smoke tests for <see cref="CommandOrbit" /> component.
/// </summary>
[AllureSuite("Refraction")]
[AllureSubSuite("Molecules")]
public sealed class CommandOrbitTests
{
    /// <summary>
    ///     CommandOrbit has AdditionalAttributes parameter with CaptureUnmatchedValues.
    /// </summary>
    [Fact]
    [AllureFeature("CommandOrbit")]
    public void CommandOrbitHasAdditionalAttributesParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(CommandOrbit).GetProperty("AdditionalAttributes");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.True(attr!.CaptureUnmatchedValues);
    }

    /// <summary>
    ///     CommandOrbit has ChildContent parameter.
    /// </summary>
    [Fact]
    [AllureFeature("CommandOrbit")]
    public void CommandOrbitHasChildContentParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(CommandOrbit).GetProperty("ChildContent");

        // Assert
        Assert.NotNull(prop);
        Assert.Equal(typeof(RenderFragment), prop!.PropertyType);
        ParameterAttribute? attr = prop.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     CommandOrbit has OnActionSelected EventCallback.
    /// </summary>
    [Fact]
    [AllureFeature("CommandOrbit")]
    public void CommandOrbitHasOnActionSelectedEventCallback()
    {
        // Arrange
        PropertyInfo? prop = typeof(CommandOrbit).GetProperty("OnActionSelected");

        // Assert
        Assert.NotNull(prop);
        Assert.True(prop!.PropertyType.IsGenericType);
        ParameterAttribute? attr = prop.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     CommandOrbit has OnDismiss EventCallback.
    /// </summary>
    [Fact]
    [AllureFeature("CommandOrbit")]
    public void CommandOrbitHasOnDismissEventCallback()
    {
        // Arrange
        PropertyInfo? prop = typeof(CommandOrbit).GetProperty("OnDismiss");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     CommandOrbit has State parameter.
    /// </summary>
    [Fact]
    [AllureFeature("CommandOrbit")]
    public void CommandOrbitHasStateParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(CommandOrbit).GetProperty("State");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     CommandOrbit inherits from ComponentBase.
    /// </summary>
    [Fact]
    [AllureFeature("CommandOrbit")]
    public void CommandOrbitInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(CommandOrbit)));
    }

    /// <summary>
    ///     CommandOrbit State defaults to Latent.
    /// </summary>
    [Fact]
    [AllureFeature("CommandOrbit")]
    public void CommandOrbitStateDefaultsToLatent()
    {
        // Arrange
        CommandOrbit orbit = new();

        // Assert
        Assert.Equal(RefractionStates.Latent, orbit.State);
    }
}