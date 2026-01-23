using System.Reflection;

using Allure.Xunit.Attributes;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Organisms;


namespace Mississippi.Refraction.L0Tests.Components.Organisms;

/// <summary>
///     Smoke tests for <see cref="Pane" /> component.
/// </summary>
[AllureSuite("Refraction")]
[AllureSubSuite("Organisms")]
public sealed class PaneTests
{
    /// <summary>
    ///     Pane has Title parameter.
    /// </summary>
    [Fact]
    [AllureFeature("Pane")]
    public void PaneHasTitleParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(Pane).GetProperty("Title");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     Pane has Variant parameter.
    /// </summary>
    [Fact]
    [AllureFeature("Pane")]
    public void PaneHasVariantParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(Pane).GetProperty("Variant");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     Pane inherits from ComponentBase.
    /// </summary>
    [Fact]
    [AllureFeature("Pane")]
    public void PaneInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(Pane)));
    }
}