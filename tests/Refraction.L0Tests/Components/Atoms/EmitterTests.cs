using System.Reflection;

using Allure.Xunit.Attributes;

using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Mississippi.Refraction.Components.Atoms;


namespace Mississippi.Refraction.L0Tests.Components.Atoms;

/// <summary>
///     Smoke tests for <see cref="Emitter" /> component.
/// </summary>
[AllureSuite("Refraction")]
[AllureSubSuite("Atoms")]
public sealed class EmitterTests : BunitContext
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
    ///     Emitter invokes OnActivate when clicked.
    /// </summary>
    [Fact]
    [AllureFeature("Emitter")]
    public void EmitterInvokesOnActivateWhenClicked()
    {
        // Arrange
        bool wasActivated = false;
        MouseEventArgs? receivedArgs = null;
        using IRenderedComponent<Emitter> cut = Render<Emitter>(p => p.Add(
            c => c.OnActivate,
            args =>
            {
                wasActivated = true;
                receivedArgs = args;
            }));

        // Act
        cut.Find(".rf-emitter").Click();

        // Assert
        Assert.True(wasActivated);
        Assert.NotNull(receivedArgs);
    }

    /// <summary>
    ///     Emitter invokes OnFocus when focused.
    /// </summary>
    [Fact]
    [AllureFeature("Emitter")]
    public void EmitterInvokesOnFocusWhenFocused()
    {
        // Arrange
        bool wasFocused = false;
        FocusEventArgs? receivedArgs = null;
        using IRenderedComponent<Emitter> cut = Render<Emitter>(p => p.Add(
            c => c.OnFocus,
            args =>
            {
                wasFocused = true;
                receivedArgs = args;
            }));

        // Act
        cut.Find(".rf-emitter").Focus();

        // Assert
        Assert.True(wasFocused);
        Assert.NotNull(receivedArgs);
    }

    /// <summary>
    ///     Emitter renders additional attributes.
    /// </summary>
    [Fact]
    [AllureFeature("Emitter")]
    public void EmitterRendersAdditionalAttributes()
    {
        // Act
        using IRenderedComponent<Emitter> cut = Render<Emitter>(p => p.AddUnmatched("data-testid", "emitter-1"));

        // Assert
        Assert.Equal("emitter-1", cut.Find(".rf-emitter").GetAttribute("data-testid"));
    }

    /// <summary>
    ///     Emitter renders custom state.
    /// </summary>
    [Fact]
    [AllureFeature("Emitter")]
    public void EmitterRendersCustomState()
    {
        // Act
        using IRenderedComponent<Emitter> cut = Render<Emitter>(p => p.Add(c => c.State, RefractionStates.Active));

        // Assert
        string? dataState = cut.Find(".rf-emitter").GetAttribute("data-state");
        Assert.Equal("active", dataState);
    }

    /// <summary>
    ///     Emitter renders seed indicator.
    /// </summary>
    [Fact]
    [AllureFeature("Emitter")]
    public void EmitterRendersSeedIndicator()
    {
        // Act
        using IRenderedComponent<Emitter> cut = Render<Emitter>();

        // Assert
        Assert.NotEmpty(cut.FindAll(".rf-emitter__seed"));
    }

    /// <summary>
    ///     Emitter renders with button role for accessibility.
    /// </summary>
    [Fact]
    [AllureFeature("Emitter")]
    public void EmitterRendersWithButtonRoleForAccessibility()
    {
        // Act
        using IRenderedComponent<Emitter> cut = Render<Emitter>();

        // Assert
        string? role = cut.Find(".rf-emitter").GetAttribute("role");
        Assert.Equal("button", role);
    }

    /// <summary>
    ///     Emitter renders with default state.
    /// </summary>
    [Fact]
    [AllureFeature("Emitter")]
    public void EmitterRendersWithDefaultState()
    {
        // Act
        using IRenderedComponent<Emitter> cut = Render<Emitter>();

        // Assert
        string? dataState = cut.Find(".rf-emitter").GetAttribute("data-state");
        Assert.Equal("idle", dataState);
    }

    /// <summary>
    ///     Emitter renders with tabindex for keyboard accessibility.
    /// </summary>
    [Fact]
    [AllureFeature("Emitter")]
    public void EmitterRendersWithTabindexForKeyboardAccessibility()
    {
        // Act
        using IRenderedComponent<Emitter> cut = Render<Emitter>();

        // Assert
        string? tabindex = cut.Find(".rf-emitter").GetAttribute("tabindex");
        Assert.Equal("0", tabindex);
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