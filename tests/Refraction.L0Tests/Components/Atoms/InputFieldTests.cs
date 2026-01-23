using System;
using System.Reflection;

using Allure.Xunit.Attributes;

using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Atoms;


namespace Mississippi.Refraction.L0Tests.Components.Atoms;

/// <summary>
///     Smoke tests for <see cref="InputField" /> component.
/// </summary>
[AllureSuite("Refraction")]
[AllureSubSuite("Atoms")]
public sealed class InputFieldTests : BunitContext
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

    /// <summary>
    ///     InputField renders with default state.
    /// </summary>
    [Fact]
    [AllureFeature("InputField")]
    public void InputFieldRendersWithDefaultState()
    {
        // Act
        using var cut = Render<InputField>();

        // Assert
        string? dataState = cut.Find(".rf-input-field").GetAttribute("data-state");
        Assert.Equal("idle", dataState);
    }

    /// <summary>
    ///     InputField renders label when provided.
    /// </summary>
    [Fact]
    [AllureFeature("InputField")]
    public void InputFieldRendersLabelWhenProvided()
    {
        // Act
        using var cut = Render<InputField>(p => p
            .Add(c => c.Label, "Test Label")
            .Add(c => c.Id, "test-id"));

        // Assert
        string textContent = cut.Find(".rf-input-field__label").TextContent;
        Assert.Contains("Test Label", textContent, StringComparison.Ordinal);
    }

    /// <summary>
    ///     InputField does not render label when empty.
    /// </summary>
    [Fact]
    [AllureFeature("InputField")]
    public void InputFieldDoesNotRenderLabelWhenEmpty()
    {
        // Act
        using var cut = Render<InputField>(p => p
            .Add(c => c.Label, string.Empty));

        // Assert
        Assert.Empty(cut.FindAll(".rf-input-field__label"));
    }

    /// <summary>
    ///     InputField renders custom state.
    /// </summary>
    [Fact]
    [AllureFeature("InputField")]
    public void InputFieldRendersCustomState()
    {
        // Act
        using var cut = Render<InputField>(p => p
            .Add(c => c.State, RefractionStates.Active));

        // Assert
        string? dataState = cut.Find(".rf-input-field").GetAttribute("data-state");
        Assert.Equal("active", dataState);
    }

    /// <summary>
    ///     InputField renders additional attributes.
    /// </summary>
    [Fact]
    [AllureFeature("InputField")]
    public void InputFieldRendersAdditionalAttributes()
    {
        // Act
        using var cut = Render<InputField>(p => p
            .AddUnmatched("data-testid", "input-1"));

        // Assert
        Assert.Equal("input-1", cut.Find(".rf-input-field").GetAttribute("data-testid"));
    }
}