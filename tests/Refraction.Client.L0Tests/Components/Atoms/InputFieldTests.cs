using System;
using System.Reflection;

using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Components.Atoms;


namespace Mississippi.Refraction.L0Tests.Components.Atoms;

/// <summary>
///     Smoke tests for <see cref="InputField" /> component.
/// </summary>
public sealed class InputFieldTests : BunitContext
{
    /// <summary>
    ///     InputField associates label with input via Id.
    /// </summary>
    [Fact]
    public void InputFieldAssociatesLabelWithInputViaId()
    {
        // Act
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p
            .Add(c => c.Label, "Username")
            .Add(c => c.Id, "username-field"));

        // Assert
        Assert.Equal("username-field", cut.Find(".rf-input-field__label").GetAttribute("for"));
        Assert.Equal("username-field", cut.Find(".rf-input-field__input").GetAttribute("id"));
    }

    /// <summary>
    ///     InputField does not render label when empty.
    /// </summary>
    [Fact]
    public void InputFieldDoesNotRenderLabelWhenEmpty()
    {
        // Act
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p.Add(c => c.Label, string.Empty));

        // Assert
        Assert.Empty(cut.FindAll(".rf-input-field__label"));
    }

    /// <summary>
    ///     InputField has AdditionalAttributes parameter with CaptureUnmatchedValues.
    /// </summary>
    [Fact]
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
    public void InputFieldInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(InputField)));
    }

    /// <summary>
    ///     InputField invokes OnBlur when input loses focus.
    /// </summary>
    [Fact]
    public void InputFieldInvokesOnBlurWhenInputLosesFocus()
    {
        // Arrange
        bool wasBlurred = false;
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p.Add(
            c => c.OnBlur,
            _ => { wasBlurred = true; }));

        // Act
        cut.Find(".rf-input-field__input").Blur();

        // Assert
        Assert.True(wasBlurred);
    }

    /// <summary>
    ///     InputField invokes OnFocus when input receives focus.
    /// </summary>
    [Fact]
    public void InputFieldInvokesOnFocusWhenInputReceivesFocus()
    {
        // Arrange
        bool wasFocused = false;
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p.Add(
            c => c.OnFocus,
            _ => { wasFocused = true; }));

        // Act
        cut.Find(".rf-input-field__input").Focus();

        // Assert
        Assert.True(wasFocused);
    }

    /// <summary>
    ///     InputField invokes ValueChanged when input changes.
    /// </summary>
    [Fact]
    public void InputFieldInvokesValueChangedWhenInputChanges()
    {
        // Arrange
        string? receivedValue = null;
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p.Add(
            c => c.ValueChanged,
            value => { receivedValue = value; }));

        // Act
        cut.Find(".rf-input-field__input").Input("new value");

        // Assert
        Assert.Equal("new value", receivedValue);
    }

    /// <summary>
    ///     InputField renders additional attributes.
    /// </summary>
    [Fact]
    public void InputFieldRendersAdditionalAttributes()
    {
        // Act
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p.AddUnmatched("data-testid", "input-1"));

        // Assert
        Assert.Equal("input-1", cut.Find(".rf-input-field").GetAttribute("data-testid"));
    }

    /// <summary>
    ///     InputField renders custom state.
    /// </summary>
    [Fact]
    public void InputFieldRendersCustomState()
    {
        // Act
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p.Add(
            c => c.State,
            RefractionStates.Active));

        // Assert
        string? dataState = cut.Find(".rf-input-field").GetAttribute("data-state");
        Assert.Equal("active", dataState);
    }

    /// <summary>
    ///     InputField renders disabled state correctly.
    /// </summary>
    [Fact]
    public void InputFieldRendersDisabledStateCorrectly()
    {
        // Act
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p.Add(c => c.IsDisabled, true));

        // Assert
        Assert.True(cut.Find(".rf-input-field__input").HasAttribute("disabled"));
    }

    /// <summary>
    ///     InputField renders input type correctly.
    /// </summary>
    [Fact]
    public void InputFieldRendersInputTypeCorrectly()
    {
        // Act
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p.Add(c => c.Type, "password"));

        // Assert
        Assert.Equal("password", cut.Find(".rf-input-field__input").GetAttribute("type"));
    }

    /// <summary>
    ///     InputField renders label when provided.
    /// </summary>
    [Fact]
    public void InputFieldRendersLabelWhenProvided()
    {
        // Act
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p
            .Add(c => c.Label, "Test Label")
            .Add(c => c.Id, "test-id"));

        // Assert
        string textContent = cut.Find(".rf-input-field__label").TextContent;
        Assert.Contains("Test Label", textContent, StringComparison.Ordinal);
    }

    /// <summary>
    ///     InputField renders placeholder correctly.
    /// </summary>
    [Fact]
    public void InputFieldRendersPlaceholderCorrectly()
    {
        // Act
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p.Add(c => c.Placeholder, "Enter value"));

        // Assert
        Assert.Equal("Enter value", cut.Find(".rf-input-field__input").GetAttribute("placeholder"));
    }

    /// <summary>
    ///     InputField renders readonly state correctly.
    /// </summary>
    [Fact]
    public void InputFieldRendersReadOnlyStateCorrectly()
    {
        // Act
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p.Add(c => c.IsReadOnly, true));

        // Assert
        Assert.True(cut.Find(".rf-input-field__input").HasAttribute("readonly"));
    }

    /// <summary>
    ///     InputField renders value correctly.
    /// </summary>
    [Fact]
    public void InputFieldRendersValueCorrectly()
    {
        // Act
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p.Add(c => c.Value, "Initial value"));

        // Assert
        Assert.Equal("Initial value", cut.Find(".rf-input-field__input").GetAttribute("value"));
    }

    /// <summary>
    ///     InputField renders with default state.
    /// </summary>
    [Fact]
    public void InputFieldRendersWithDefaultState()
    {
        // Act
        using IRenderedComponent<InputField> cut = Render<InputField>();

        // Assert
        string? dataState = cut.Find(".rf-input-field").GetAttribute("data-state");
        Assert.Equal("idle", dataState);
    }

    /// <summary>
    ///     InputField State defaults to Idle.
    /// </summary>
    [Fact]
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
    public void InputFieldTypeDefaultsToText()
    {
        // Arrange
        InputField inputField = new();

        // Assert
        Assert.Equal("text", inputField.Type);
    }
}