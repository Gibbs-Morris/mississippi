using System;
using System.Collections.Generic;

using AngleSharp.Dom;

using Bunit;

using Microsoft.AspNetCore.Components.Web;

using Mississippi.Refraction.Client.Components.Atoms.Activation;


namespace Mississippi.Refraction.Client.L0Tests.Components.Atoms.Activation;

/// <summary>
///     Verifies activation, forwarding and disabled-state behavior for <see cref="Emitter" />.
/// </summary>
public sealed class EmitterBehaviorTests : BunitContext
{
    /// <summary>
    ///     Emitter permits a caller-provided accessible name when no visible label is rendered.
    /// </summary>
    [Fact]
    public void EmitterAllowsIconOnlyAccessibleName()
    {
        // Arrange
        using IRenderedComponent<Emitter> cut = Render<Emitter>(p => p.AddUnmatched("aria-label", "Open palette"));

        // Assert
        IElement button = cut.Find(".rf-emitter");
        Assert.Equal("Open palette", button.GetAttribute("aria-label"));
        Assert.Empty(cut.FindAll(".rf-emitter__label"));
    }

    /// <summary>
    ///     Emitter combines disabled inputs, renders the effective state and guards callbacks.
    /// </summary>
    /// <param name="isDisabled">Whether the explicit disabled parameter is set.</param>
    /// <param name="state">The supplied visual state.</param>
    [Theory]
    [InlineData(true, RefractionStates.Idle)]
    [InlineData(false, RefractionStates.Disabled)]
    public void EmitterDisabledStateGuardsInteraction(
        bool isDisabled,
        string state
    )
    {
        // Arrange
        int activationCount = 0;
        int focusCount = 0;
        using IRenderedComponent<Emitter> cut = Render<Emitter>(p => p
            .Add(c => c.IsDisabled, isDisabled)
            .Add(c => c.State, state)
            .Add(c => c.OnActivate, _ => activationCount++)
            .Add(c => c.OnFocus, _ => focusCount++));
        IElement button = cut.Find("button.rf-emitter");

        // Act
        button.Click();
        button.Focus();

        // Assert
        Assert.True(button.HasAttribute("disabled"));
        Assert.Equal(RefractionStates.Disabled, button.GetAttribute("data-state"));
        Assert.Equal("true", button.GetAttribute("aria-disabled"));
        Assert.Equal(0, activationCount);
        Assert.Equal(0, focusCount);

        // Act
        cut.Render(p => p.Add(c => c.IsDisabled, false).Add(c => c.State, RefractionStates.Active));
        button = cut.Find("button.rf-emitter");
        button.Click();
        button.Focus();

        // Assert
        Assert.False(button.HasAttribute("disabled"));
        Assert.Equal(RefractionStates.Active, button.GetAttribute("data-state"));
        Assert.Equal("false", button.GetAttribute("aria-disabled"));
        Assert.Equal(1, activationCount);
        Assert.Equal(1, focusCount);
    }

    /// <summary>
    ///     Emitter forwards ordinary attributes and composes component and caller classes.
    /// </summary>
    [Fact]
    public void EmitterForwardsAttributesAndComposesClasses()
    {
        // Arrange
        IReadOnlyDictionary<string, object> attributes = new Dictionary<string, object>
        {
            ["CLASS"] = "caller-class",
            ["aria-label"] = "Open command palette",
            ["data-testid"] = "emitter-1",
        };
        using IRenderedComponent<Emitter> cut = Render<Emitter>(p => p
            .Add(c => c.Class, "application-emitter")
            .Add(c => c.AdditionalAttributes, attributes));

        // Assert
        IElement button = cut.Find("button.rf-emitter");
        Assert.Equal("Open command palette", button.GetAttribute("aria-label"));
        Assert.Equal("emitter-1", button.GetAttribute("data-testid"));
        Assert.Contains("rf-emitter", button.ClassName, StringComparison.Ordinal);
        Assert.Contains("application-emitter", button.ClassName, StringComparison.Ordinal);
        Assert.Contains("caller-class", button.ClassName, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Emitter forwards case-distinct safe attributes without a dictionary collision.
    /// </summary>
    [Fact]
    public void EmitterForwardsCaseDistinctSafeAttributes()
    {
        // Arrange
        IReadOnlyDictionary<string, object> attributes = new Dictionary<string, object>
        {
            ["data-note"] = "lower",
            ["DATA-NOTE"] = "upper",
        };

        // Act
        using IRenderedComponent<Emitter> cut = Render<Emitter>(p => p.Add(c => c.AdditionalAttributes, attributes));

        // Assert
        Assert.True(cut.Find("button.rf-emitter").HasAttribute("data-note"));
    }

    /// <summary>
    ///     Emitter keeps controlled attributes authoritative over unmatched values.
    /// </summary>
    [Fact]
    public void EmitterKeepsControlledAttributes()
    {
        // Act
        using IRenderedComponent<Emitter> cut = Render<Emitter>(p => p
            .Add(c => c.Label, "Emit signal")
            .AddUnmatched("type", "submit")
            .AddUnmatched("disabled", true)
            .AddUnmatched("data-state", RefractionStates.Active)
            .AddUnmatched("aria-disabled", false)
            .AddUnmatched("onclick", "return false;"));

        // Assert
        IElement button = cut.Find("button.rf-emitter");
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.False(button.HasAttribute("disabled"));
        Assert.Equal(RefractionStates.Idle, button.GetAttribute("data-state"));
        Assert.Equal("false", button.GetAttribute("aria-disabled"));
        Assert.DoesNotContain("return false", button.OuterHtml, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Emitter preserves the typed activation callback argument.
    /// </summary>
    [Fact]
    public void EmitterPreservesOnActivateArguments()
    {
        // Arrange
        MouseEventArgs? receivedArgs = null;
        using IRenderedComponent<Emitter> cut = Render<Emitter>(p => p.Add(
            c => c.OnActivate,
            args => receivedArgs = args));
        MouseEventArgs expectedArgs = new()
        {
            Button = 1,
            ClientX = 42,
        };

        // Act
        cut.Find(".rf-emitter").Click(expectedArgs);

        // Assert
        Assert.Same(expectedArgs, receivedArgs);
    }
}