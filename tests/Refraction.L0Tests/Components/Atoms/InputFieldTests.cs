using System.Reflection;

using Allure.Xunit.Attributes;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Atoms;


namespace Mississippi.Refraction.L0Tests.Components.Atoms;

/// <summary>
///     Smoke tests for <see cref="InputField" /> component.
/// </summary>
[AllureSuite("Refraction")]
[AllureSubSuite("Atoms")]
public sealed class InputFieldTests
{
    /// <summary>
    ///     InputField has Label parameter.
    /// </summary>
    [Fact]
    [AllureFeature("InputField")]
    public void InputFieldHasLabelParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(InputField).GetProperty("Label");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     InputField has ValueChanged EventCallback.
    /// </summary>
    [Fact]
    [AllureFeature("InputField")]
    public void InputFieldHasValueChangedEventCallback()
    {
        // Arrange
        PropertyInfo? prop = typeof(InputField).GetProperty("ValueChanged");

        // Assert
        Assert.NotNull(prop);
        Assert.True(prop!.PropertyType.IsGenericType);
    }

    /// <summary>
    ///     InputField inherits from ComponentBase.
    /// </summary>
    [Fact]
    [AllureFeature("InputField")]
    public void InputFieldInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(InputField)));
    }
}