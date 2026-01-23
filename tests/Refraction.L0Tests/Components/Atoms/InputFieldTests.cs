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
    ///     InputField has AdditionalAttributes parameter with CaptureUnmatchedValues.
    /// </summary>
    [Fact]
    [AllureFeature("InputField")]
    public void InputFieldHasAdditionalAttributesParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(InputField).GetProperty("AdditionalAttributes");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.True(attr!.CaptureUnmatchedValues);
    }

    /// <summary>
    ///     InputField has Id parameter.
    /// </summary>
    [Fact]
    [AllureFeature("InputField")]
    public void InputFieldHasIdParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(InputField).GetProperty("Id");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     InputField has IsDisabled parameter.
    /// </summary>
    [Fact]
    [AllureFeature("InputField")]
    public void InputFieldHasIsDisabledParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(InputField).GetProperty("IsDisabled");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     InputField has IsReadOnly parameter.
    /// </summary>
    [Fact]
    [AllureFeature("InputField")]
    public void InputFieldHasIsReadOnlyParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(InputField).GetProperty("IsReadOnly");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

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
    ///     InputField has OnBlur EventCallback.
    /// </summary>
    [Fact]
    [AllureFeature("InputField")]
    public void InputFieldHasOnBlurEventCallback()
    {
        // Arrange
        PropertyInfo? prop = typeof(InputField).GetProperty("OnBlur");

        // Assert
        Assert.NotNull(prop);
        Assert.True(prop!.PropertyType.IsGenericType);
        ParameterAttribute? attr = prop.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     InputField has OnFocus EventCallback.
    /// </summary>
    [Fact]
    [AllureFeature("InputField")]
    public void InputFieldHasOnFocusEventCallback()
    {
        // Arrange
        PropertyInfo? prop = typeof(InputField).GetProperty("OnFocus");

        // Assert
        Assert.NotNull(prop);
        Assert.True(prop!.PropertyType.IsGenericType);
        ParameterAttribute? attr = prop.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     InputField has Placeholder parameter.
    /// </summary>
    [Fact]
    [AllureFeature("InputField")]
    public void InputFieldHasPlaceholderParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(InputField).GetProperty("Placeholder");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     InputField has State parameter.
    /// </summary>
    [Fact]
    [AllureFeature("InputField")]
    public void InputFieldHasStateParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(InputField).GetProperty("State");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     InputField has Type parameter.
    /// </summary>
    [Fact]
    [AllureFeature("InputField")]
    public void InputFieldHasTypeParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(InputField).GetProperty("Type");

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
        ParameterAttribute? attr = prop.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     InputField has Value parameter.
    /// </summary>
    [Fact]
    [AllureFeature("InputField")]
    public void InputFieldHasValueParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(InputField).GetProperty("Value");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
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

    /// <summary>
    ///     InputField State defaults to Idle.
    /// </summary>
    [Fact]
    [AllureFeature("InputField")]
    public void InputFieldStateDefaultsToIdle()
    {
        // Arrange
        InputField inputField = new();

        // Assert
        Assert.Equal(RefractionStates.Idle, inputField.State);
    }

    /// <summary>
    ///     InputField Type defaults to text.
    /// </summary>
    [Fact]
    [AllureFeature("InputField")]
    public void InputFieldTypeDefaultsToText()
    {
        // Arrange
        InputField inputField = new();

        // Assert
        Assert.Equal("text", inputField.Type);
    }
}