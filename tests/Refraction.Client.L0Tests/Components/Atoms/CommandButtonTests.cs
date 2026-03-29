using System;
using System.Reflection;

using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Client.Components.Atoms;


namespace Mississippi.Refraction.Client.L0Tests.Components.Atoms;

/// <summary>
///     Tests for <see cref="CommandButton" /> component.
/// </summary>
public sealed class CommandButtonTests : BunitContext
{
    /// <summary>
    ///     CommandButton has AdditionalAttributes parameter with CaptureUnmatchedValues.
    /// </summary>
    [Fact]
    public void CommandButtonHasAdditionalAttributesParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(CommandButton).GetProperty("AdditionalAttributes");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.True(attr!.CaptureUnmatchedValues);
    }

    /// <summary>
    ///     CommandButton has IsDisabled parameter.
    /// </summary>
    [Fact]
    public void CommandButtonHasIsDisabledParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(CommandButton).GetProperty("IsDisabled");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(bool), prop.PropertyType);
    }

    /// <summary>
    ///     CommandButton has Label parameter.
    /// </summary>
    [Fact]
    public void CommandButtonHasLabelParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(CommandButton).GetProperty("Label");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(string), prop.PropertyType);
    }

    /// <summary>
    ///     CommandButton has OnClick event callback.
    /// </summary>
    [Fact]
    public void CommandButtonHasOnClickEventCallback()
    {
        // Arrange
        PropertyInfo? prop = typeof(CommandButton).GetProperty("OnClick");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(typeof(EventCallback), prop.PropertyType);
    }

    /// <summary>
    ///     CommandButton has State parameter.
    /// </summary>
    [Fact]
    public void CommandButtonHasStateParameter()
    {
        // Arrange
        PropertyInfo? prop = typeof(CommandButton).GetProperty("State");

        // Assert
        Assert.NotNull(prop);
        ParameterAttribute? attr = prop!.GetCustomAttribute<ParameterAttribute>();
        Assert.NotNull(attr);
    }

    /// <summary>
    ///     CommandButton inherits from ComponentBase.
    /// </summary>
    [Fact]
    public void CommandButtonInheritsFromComponentBase()
    {
        // Assert
        Assert.True(typeof(ComponentBase).IsAssignableFrom(typeof(CommandButton)));
    }

    /// <summary>
    ///     CommandButton invokes OnClick when pressed.
    /// </summary>
    [Fact]
    public void CommandButtonInvokesOnClickWhenPressed()
    {
        // Arrange
        bool wasClicked = false;
        using IRenderedComponent<CommandButton> cut = Render<CommandButton>(parameters => parameters
            .Add(component => component.Label, "Apply action")
            .Add(component => component.OnClick, () => wasClicked = true));

        // Act
        cut.Find(".rf-command-button").Click();

        // Assert
        Assert.True(wasClicked);
    }

    /// <summary>
    ///     CommandButton merges an additional CSS class with the component root class.
    /// </summary>
    [Fact]
    public void CommandButtonMergesAdditionalCssClassWithRootClass()
    {
        // Act
        using IRenderedComponent<CommandButton> cut = Render<CommandButton>(parameters => parameters
            .Add(component => component.Label, "Apply action")
            .AddUnmatched("class", "command-host"));

        // Assert
        Assert.NotNull(cut.Find(".rf-command-button.command-host"));
    }

    /// <summary>
    ///     CommandButton renders additional attributes.
    /// </summary>
    [Fact]
    public void CommandButtonRendersAdditionalAttributes()
    {
        // Act
        using IRenderedComponent<CommandButton> cut = Render<CommandButton>(parameters => parameters
            .Add(component => component.Label, "Apply action")
            .AddUnmatched("data-testid", "command-button-1"));

        // Assert
        Assert.Equal("command-button-1", cut.Find(".rf-command-button").GetAttribute("data-testid"));
    }

    /// <summary>
    ///     CommandButton renders custom state.
    /// </summary>
    [Fact]
    public void CommandButtonRendersCustomState()
    {
        // Act
        using IRenderedComponent<CommandButton> cut = Render<CommandButton>(parameters => parameters
            .Add(component => component.Label, "Apply action")
            .Add(component => component.State, RefractionStates.Active));

        // Assert
        Assert.Equal("active", cut.Find(".rf-command-button").GetAttribute("data-state"));
    }

    /// <summary>
    ///     CommandButton renders disabled state.
    /// </summary>
    [Fact]
    public void CommandButtonRendersDisabledState()
    {
        // Act
        using IRenderedComponent<CommandButton> cut = Render<CommandButton>(parameters => parameters
            .Add(component => component.Label, "Review and edit")
            .Add(component => component.IsDisabled, true));

        // Assert
        Assert.True(cut.Find(".rf-command-button").HasAttribute("disabled"));
    }

    /// <summary>
    ///     CommandButton renders the label.
    /// </summary>
    [Fact]
    public void CommandButtonRendersLabel()
    {
        // Act
        using IRenderedComponent<CommandButton> cut = Render<CommandButton>(parameters => parameters.Add(
            component => component.Label,
            "Save review"));

        // Assert
        Assert.Contains("Save review", cut.Find(".rf-command-button").TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    ///     CommandButton renders with default state.
    /// </summary>
    [Fact]
    public void CommandButtonRendersWithDefaultState()
    {
        // Act
        using IRenderedComponent<CommandButton> cut = Render<CommandButton>(parameters => parameters.Add(
            component => component.Label,
            "Apply action"));

        // Assert
        Assert.Equal("idle", cut.Find(".rf-command-button").GetAttribute("data-state"));
    }

    /// <summary>
    ///     CommandButton State defaults to Idle.
    /// </summary>
    [Fact]
    public void CommandButtonStateDefaultsToIdle()
    {
        // Arrange
        CommandButton component = new();

        // Assert
        Assert.Equal(RefractionStates.Idle, component.State);
    }
}