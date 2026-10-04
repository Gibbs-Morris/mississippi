using System.Collections.Generic;

using Bunit;

using Mississippi.Refraction.Client.Components.Atoms.Input;


namespace Mississippi.Refraction.Client.L0Tests.Components.Atoms.Input;

/// <summary>Verifies associated field guidance and parent-controlled validation feedback.</summary>
public sealed class InputFieldFeedbackTests : BunitContext
{
    /// <summary>Empty feedback never creates empty descriptions or dangling references.</summary>
    /// <param name="text">Absent or blank feedback.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankFeedbackDoesNotCreateDescriptions(
        string? text
    )
    {
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p
            .Add(c => c.State, RefractionStates.Invalid)
            .Add(c => c.HelperText, text)
            .Add(c => c.ErrorText, text));
        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
        Assert.False(cut.Find("input").HasAttribute("aria-describedby"));
        Assert.Empty(cut.FindAll("p"));
    }

    /// <summary>Changing identity updates every generated association together.</summary>
    [Fact]
    public void IdentityChangesKeepFeedbackAssociated()
    {
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p
            .Add(c => c.HelperText, "Use a work address.")
            .Add(c => c.ErrorText, "Check the address.")
            .Add(c => c.State, RefractionStates.Error));
        string? generatedId = cut.Find("input").Id;
        Assert.Equal($"{generatedId}-helper {generatedId}-error", cut.Find("input").GetAttribute("aria-describedby"));
        cut.Render(p => p.Add(c => c.Id, "custom"));
        Assert.Equal("custom-helper custom-error", cut.Find("input").GetAttribute("aria-describedby"));
        Assert.Single(cut.FindAll("#custom-helper"));
        Assert.Single(cut.FindAll("#custom-error"));
        Assert.Empty(cut.FindAll($"[id='{generatedId}-helper']"));
        Assert.Empty(cut.FindAll($"[id='{generatedId}-error']"));
    }

    /// <summary>Invalid states expose the error and retain helper and caller descriptions.</summary>
    /// <param name="state">The invalid component state.</param>
    [Theory]
    [InlineData(RefractionStates.Invalid)]
    [InlineData(RefractionStates.Error)]
    public void InvalidStatesAssociateAllFeedback(
        string state
    )
    {
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p.Add(c => c.Id, "email")
            .Add(c => c.State, state)
            .Add(c => c.HelperText, "Use your work address.")
            .Add(c => c.ErrorText, "Enter a valid email address.")
            .Add(
                c => c.InputAttributes,
                new Dictionary<string, object>
                {
                    ["aria-describedby"] = "privacy format",
                    ["aria-invalid"] = "false",
                    ["required"] = true,
                }));
        Assert.Equal("privacy format email-helper email-error", cut.Find("input").GetAttribute("aria-describedby"));
        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
        Assert.True(cut.Find("input").HasAttribute("required"));
        Assert.Equal("Use your work address.", cut.Find("#email-helper").TextContent);
        Assert.Equal("Enter a valid email address.", cut.Find("#email-error").TextContent);
        Assert.Equal("alert", cut.Find("#email-error").GetAttribute("role"));
    }

    /// <summary>Native accessibility settings retain their HTML case-insensitive behavior.</summary>
    [Fact]
    public void NativeAccessibilityAttributesRemainSupported()
    {
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p.Add(c => c.HelperText, "Guidance")
            .Add(c => c.Id, "field")
            .Add(
                c => c.InputAttributes,
                new Dictionary<string, object>
                {
                    ["ARIA-DESCRIBEDBY"] = "external",
                    ["ARIA-INVALID"] = "grammar",
                }));
        Assert.Equal("external field-helper", cut.Find("input").GetAttribute("aria-describedby"));
        Assert.Equal("grammar", cut.Find("input").GetAttribute("aria-invalid"));
        cut.Render(p => p.Add(
            c => c.InputAttributes,
            new Dictionary<string, object>
            {
                ["aria-describedby"] = null!,
                ["aria-invalid"] = null!,
            }));
        Assert.Equal("field-helper", cut.Find("input").GetAttribute("aria-describedby"));
        Assert.False(cut.Find("input").HasAttribute("aria-invalid"));
    }

    /// <summary>Removing text clears its association without resetting the parent's invalid state.</summary>
    [Fact]
    public void RemovingFeedbackClearsGeneratedDescriptions()
    {
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p
            .Add(c => c.State, RefractionStates.Invalid)
            .Add(c => c.HelperText, "Guidance")
            .Add(c => c.ErrorText, "Error"));
        cut.Render(p => p.Add(c => c.HelperText, null).Add(c => c.ErrorText, null));
        Assert.Empty(cut.FindAll("p"));
        Assert.False(cut.Find("input").HasAttribute("aria-describedby"));
        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
    }

    /// <summary>Feedback remains available on disabled and read-only fields.</summary>
    /// <param name="isDisabled">Whether the input is disabled.</param>
    /// <param name="isReadOnly">Whether the input is read-only.</param>
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void RestrictedFieldsRetainFeedback(
        bool isDisabled,
        bool isReadOnly
    )
    {
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p
            .Add(c => c.IsDisabled, isDisabled)
            .Add(c => c.IsReadOnly, isReadOnly)
            .Add(c => c.State, RefractionStates.Invalid)
            .Add(c => c.ErrorText, "Contact your administrator."));
        Assert.Equal(isDisabled, cut.Find("input").HasAttribute("disabled"));
        Assert.Equal(isReadOnly, cut.Find("input").HasAttribute("readonly"));
        Assert.Equal("Contact your administrator.", cut.Find("[role=alert]").TextContent);
        Assert.Equal(cut.Find("[role=alert]").Id, cut.Find("input").GetAttribute("aria-describedby"));
    }

    /// <summary>Clearing invalid state removes error references while preserving guidance.</summary>
    [Fact]
    public void ReturningToValidStateRemovesErrorFeedback()
    {
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p
            .Add(c => c.HelperText, "A work address is required.")
            .Add(c => c.ErrorText, "Check the address.")
            .Add(c => c.State, RefractionStates.Invalid));
        string? id = cut.Find("input").Id;
        cut.Render(p => p.Add(c => c.State, RefractionStates.Active));
        Assert.Equal(id, cut.Find("input").Id);
        Assert.Equal($"{id}-helper", cut.Find("input").GetAttribute("aria-describedby"));
        Assert.False(cut.Find("input").HasAttribute("aria-invalid"));
        Assert.Empty(cut.FindAll(".rf-input-field__error"));
        Assert.Equal("Check the address.", cut.Instance.ErrorText);
    }
}