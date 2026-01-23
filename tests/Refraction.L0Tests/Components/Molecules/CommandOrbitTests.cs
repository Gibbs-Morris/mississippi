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
}