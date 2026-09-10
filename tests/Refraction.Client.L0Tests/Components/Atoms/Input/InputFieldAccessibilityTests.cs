using System.Collections.Generic;

using Bunit;

using Microsoft.AspNetCore.Components;

using Mississippi.Refraction.Client.Components.Atoms.Input;


namespace Mississippi.Refraction.Client.L0Tests.Components.Atoms.Input;

/// <summary>
///     Verifies input identity and native HTML integration for accessible forms.
/// </summary>
public sealed class InputFieldAccessibilityTests : BunitContext
{
    /// <summary>Labels without explicit IDs target unique inputs.</summary>
    [Fact]
    public void DefaultLabelsTargetDistinctInputs()
    {
        using IRenderedComponent<InputField> first = Render<InputField>(p => p.Add(c => c.Label, "First name"));
        using IRenderedComponent<InputField> second = Render<InputField>(p => p.Add(c => c.Label, "Last name"));
        string? firstId = first.Find("input").Id;
        string? secondId = second.Find("input").Id;
        Assert.False(string.IsNullOrWhiteSpace(firstId));
        Assert.False(string.IsNullOrWhiteSpace(secondId));
        Assert.NotEqual(firstId, secondId);
        Assert.Equal(firstId, first.Find("label").GetAttribute("for"));
        Assert.Equal(secondId, second.Find("label").GetAttribute("for"));
    }

    /// <summary>An explicit ID takes precedence over a dictionary entry.</summary>
    [Fact]
    public void ExplicitIdentityCannotBeOverriddenByInputAttributes()
    {
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p.Add(c => c.Label, "Email")
            .Add(c => c.Id, "email")
            .Add(
                c => c.InputAttributes,
                new Dictionary<string, object>
                {
                    ["id"] = "different",
                }));
        Assert.Equal("email", cut.Find("input").Id);
        Assert.Equal("email", cut.Find("label").GetAttribute("for"));
    }

    /// <summary>Fallback identity survives rerenders and temporary caller-provided IDs.</summary>
    /// <param name="blankId">A missing or blank caller-provided ID.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FallbackIdentityRemainsStable(
        string? blankId
    )
    {
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p.Add(c => c.Label, "Email"));
        string? generatedId = cut.Find("input").Id;
        cut.Render(p => p.Add(c => c.Value, "person@example.com"));
        Assert.Equal(generatedId, cut.Find("input").Id);
        cut.Render(p => p.Add(c => c.Id, "custom"));
        Assert.Equal("custom", cut.Find("label").GetAttribute("for"));
        cut.Render(p => p.Add(c => c.Id, blankId!));
        Assert.Equal(generatedId, cut.Find("input").Id);
        Assert.Equal(generatedId, cut.Find("label").GetAttribute("for"));
    }

    /// <summary>Native attributes reach the input without changing the wrapper contract.</summary>
    [Fact]
    public void NativeAndWrapperAttributesHaveSeparateTargets()
    {
        Dictionary<string, object> attributes = new()
        {
            ["name"] = "contact-email",
            ["autocomplete"] = "email",
            ["inputmode"] = "email",
            ["aria-label"] = "Contact email",
            ["aria-describedby"] = "email-help",
            ["required"] = true,
        };
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p
            .Add(c => c.InputAttributes, attributes)
            .AddUnmatched("data-testid", "email-wrapper"));
        foreach (string name in new[] { "name", "autocomplete", "inputmode", "aria-label", "aria-describedby" })
        {
            Assert.Equal(attributes[name], cut.Find("input").GetAttribute(name));
            Assert.False(cut.Find(".rf-input-field").HasAttribute(name));
        }

        Assert.True(cut.Find("input").HasAttribute("required"));
        Assert.Equal("email-wrapper", cut.Find(".rf-input-field").GetAttribute("data-testid"));
        Assert.False(cut.Find("input").HasAttribute("data-testid"));
    }

    /// <summary>Updating and removing attributes does not leave stale native settings.</summary>
    [Fact]
    public void NativeAttributesFollowParameterUpdates()
    {
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p.Add(
            c => c.InputAttributes,
            new Dictionary<string, object>
            {
                ["autocomplete"] = "email",
            }));
        cut.Render(p => p.Add(
            c => c.InputAttributes,
            new Dictionary<string, object>
            {
                ["autocomplete"] = "username",
            }));
        Assert.Equal("username", cut.Find("input").GetAttribute("autocomplete"));
        cut.Render(p => p.Add(c => c.InputAttributes, null));
        Assert.False(cut.Find("input").HasAttribute("autocomplete"));
    }

    /// <summary>A cleared native input reports an empty value to its parent.</summary>
    [Fact]
    public void NullInputValueReportsEmptyString()
    {
        string? received = null;
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p
            .Add(c => c.Value, "original")
            .Add(c => c.ValueChanged, value => received = value));
        cut.Find("input")
            .TriggerEvent(
                "oninput",
                new ChangeEventArgs
                {
                    Value = null,
                });
        Assert.Equal(string.Empty, received);
        Assert.Equal("original", cut.Instance.Value);
    }

    /// <summary>Component parameters remain authoritative over conflicting native attributes.</summary>
    /// <param name="isRestricted">Whether the input is disabled and read-only.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OwnedAttributesCannotBeOverridden(
        bool isRestricted
    )
    {
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p.Add(c => c.Value, "controlled")
            .Add(c => c.Type, "email")
            .Add(c => c.IsDisabled, isRestricted)
            .Add(c => c.IsReadOnly, isRestricted)
            .Add(
                c => c.InputAttributes,
                new Dictionary<string, object>
                {
                    ["value"] = "override",
                    ["type"] = "password",
                    ["disabled"] = !isRestricted,
                    ["readonly"] = !isRestricted,
                }));
        Assert.Equal("controlled", cut.Find("input").GetAttribute("value"));
        Assert.Equal("email", cut.Find("input").GetAttribute("type"));
        Assert.Equal(isRestricted, cut.Find("input").HasAttribute("disabled"));
        Assert.Equal(isRestricted, cut.Find("input").HasAttribute("readonly"));
    }

    /// <summary>Typing reports intent without changing the parent-owned value.</summary>
    [Fact]
    public void ValueCallbackPreservesControlledState()
    {
        string? received = null;
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p.Add(c => c.Value, "original")
            .Add(c => c.ValueChanged, value => received = value)
            .Add(
                c => c.InputAttributes,
                new Dictionary<string, object>
                {
                    ["name"] = "email",
                }));
        cut.Find("input").Input("edited");
        Assert.Equal("edited", received);
        Assert.Equal("original", cut.Instance.Value);
    }

    /// <summary>Native attribute entries cannot replace the component's value callback.</summary>
    [Fact]
    public void ValueCallbackTakesPrecedenceOverNativeAttributes()
    {
        string? received = null;
        bool wasOverrideCalled = false;
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p
            .Add(c => c.ValueChanged, value => received = value)
            .Add(
                c => c.InputAttributes,
                new Dictionary<string, object>
                {
                    ["oninput"] = EventCallback.Factory.Create<ChangeEventArgs>(this, _ => wasOverrideCalled = true),
                }));
        cut.Find("input").Input("edited");
        Assert.Equal("edited", received);
        Assert.False(wasOverrideCalled);
    }
}