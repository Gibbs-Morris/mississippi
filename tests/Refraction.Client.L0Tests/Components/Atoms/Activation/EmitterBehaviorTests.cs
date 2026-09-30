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
    /// <summary>Emitter replaces unmatched attributes and revalidates the replacement name.</summary>
    [Fact]
    public void EmitterAcceptsAriaNameAfterAttributeReplacement()
    {
        // Arrange
        IReadOnlyDictionary<string, object> labelAttributes = new Dictionary<string, object>
        {
            ["aria-label"] = "Open palette",
        };
        using IRenderedComponent<Emitter> cut =
            Render<Emitter>(p => p.Add(c => c.AdditionalAttributes, labelAttributes));
        IReadOnlyDictionary<string, object> labelledByAttributes = new Dictionary<string, object>
        {
            ["aria-labelledby"] = "emitter-description",
        };

        // Act
        cut.Render(p => p.Add(c => c.AdditionalAttributes, labelledByAttributes));

        // Assert
        IElement button = cut.Find("button.rf-emitter");
        Assert.False(button.HasAttribute("aria-label"));
        Assert.Equal("emitter-description", button.GetAttribute("aria-labelledby"));
    }

    /// <summary>Emitter accepts either native ARIA naming source, including case variants.</summary>
    /// <param name="attributeName">The supplied ARIA naming attribute.</param>
    /// <param name="value">The nonblank naming value.</param>
    /// <param name="expectedAttributeName">The normalized DOM attribute name.</param>
    [Theory]
    [InlineData("aria-label", "Open palette", "aria-label")]
    [InlineData("ARIA-LABEL", "Open palette", "aria-label")]
    [InlineData("aria-labelledby", "emitter-description", "aria-labelledby")]
    [InlineData("ARIA-LABELLEDBY", "emitter-description", "aria-labelledby")]
    public void EmitterAcceptsAriaNameSource(
        string attributeName,
        string value,
        string expectedAttributeName
    )
    {
        // Act
        using IRenderedComponent<Emitter> cut = Render<Emitter>(p => p.AddUnmatched(attributeName, value));

        // Assert
        Assert.Equal(value, cut.Find("button.rf-emitter").GetAttribute(expectedAttributeName));
    }

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
            .AddUnmatched("aria-label", "Emit signal")
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
            ["aria-label"] = "Emit signal",
        };

        // Act
        using IRenderedComponent<Emitter> cut = Render<Emitter>(p => p.Add(c => c.AdditionalAttributes, attributes));

        // Assert
        Assert.True(cut.Find("button.rf-emitter").HasAttribute("data-note"));
    }

    /// <summary>Emitter forwards the last case-insensitive naming value that it validates.</summary>
    [Fact]
    public void EmitterForwardsLastCaseInsensitiveNameValue()
    {
        // Arrange
        IReadOnlyDictionary<string, object> attributes = new Dictionary<string, object>
        {
            ["aria-label"] = " ",
            ["ARIA-LABEL"] = "Open palette",
        };

        // Act
        using IRenderedComponent<Emitter> cut = Render<Emitter>(p => p.Add(c => c.AdditionalAttributes, attributes));

        // Assert
        Assert.Equal("Open palette", cut.Find("button.rf-emitter").GetAttribute("aria-label"));
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
        using IRenderedComponent<Emitter> cut = Render<Emitter>(p => p
            .AddUnmatched("aria-label", "Emit signal")
            .Add(c => c.OnActivate, args => receivedArgs = args));
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

    /// <summary>
    ///     Emitter rejects a blank case variant that would replace a meaningful ARIA name in the DOM.
    /// </summary>
    [Fact]
    public void EmitterRejectsBlankCaseVariantAfterMeaningfulName()
    {
        // Arrange
        IReadOnlyDictionary<string, object> attributes = new Dictionary<string, object>
        {
            ["aria-label"] = "Open palette",
            ["ARIA-LABEL"] = " ",
        };

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
        {
            using IRenderedComponent<Emitter> cut = Render<Emitter>(p => p.Add(
                c => c.AdditionalAttributes,
                attributes));
        });

        // Assert
        Assert.Contains("aria-label", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Emitter rejects non-string native ARIA naming values.
    /// </summary>
    /// <param name="attributeName">The ARIA naming attribute.</param>
    [Theory]
    [InlineData("aria-label")]
    [InlineData("aria-labelledby")]
    public void EmitterRejectsNonStringAccessibleName(
        string attributeName
    )
    {
        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
        {
            using IRenderedComponent<Emitter> cut = Render<Emitter>(p => p.AddUnmatched(attributeName, true));
        });

        // Assert
        Assert.Contains(attributeName, error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("string", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Emitter rejects whitespace-only visible labels without an accessible name.
    /// </summary>
    /// <param name="label">The blank label value.</param>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void EmitterRejectsWhitespaceLabelWithoutAccessibleName(
        string label
    )
    {
        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
        {
            using IRenderedComponent<Emitter> cut = Render<Emitter>(p => p.Add(c => c.Label, label));
        });

        // Assert
        Assert.Contains("accessible name", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Emitter rejects a render without a visible label or accessible name.
    /// </summary>
    [Fact]
    public void EmitterRequiresAccessibleName()
    {
        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
        {
            using IRenderedComponent<Emitter> cut = Render<Emitter>();
        });

        // Assert
        Assert.Contains("Label", error.Message, StringComparison.Ordinal);
        Assert.Contains("aria-label", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Emitter revalidates its accessible name when an update removes the name.
    /// </summary>
    [Fact]
    public void EmitterRevalidatesAccessibleNameOnUpdate()
    {
        // Arrange
        using IRenderedComponent<Emitter> cut = Render<Emitter>(p => p.AddUnmatched("aria-label", "Open palette"));

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
            cut.Render(p => p.AddUnmatched("aria-label", " ")));

        // Assert
        Assert.Contains("accessible name", error.Message, StringComparison.Ordinal);
    }

    /// <summary>Emitter revalidates when a visible label is removed on update.</summary>
    [Fact]
    public void EmitterRevalidatesWhenVisibleLabelIsRemoved()
    {
        // Arrange
        using IRenderedComponent<Emitter> cut = Render<Emitter>(p => p.Add(c => c.Label, "Emit signal"));

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() =>
            cut.Render(p => p.Add(c => c.Label, " ")));

        // Assert
        Assert.Contains("accessible name", error.Message, StringComparison.Ordinal);
    }
}