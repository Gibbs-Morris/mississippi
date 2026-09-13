using System.Collections.Generic;

using Bunit;

using Mississippi.Refraction.Client.Components.Atoms.Input;


namespace Mississippi.Refraction.Client.L0Tests.Components.Atoms.Input;

/// <summary>Verifies stable, unique description references.</summary>
public sealed class InputFieldDescriptionTests : BunitContext
{
    /// <summary>ID references retain case-sensitive identity.</summary>
    [Fact]
    public void DescriptionIdentityIsCaseSensitive()
    {
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p.Add(
            c => c.InputAttributes,
            new Dictionary<string, object>
            {
                ["aria-describedby"] = "Hint hint Hint",
            }));
        Assert.Equal("Hint hint", cut.Find("input").GetAttribute("aria-describedby"));
    }

    /// <summary>Caller and generated references are normalized without reordering unique IDs.</summary>
    [Fact]
    public void DescriptionReferencesAreNormalizedAndDeduplicated()
    {
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p.Add(c => c.Id, "field")
            .Add(c => c.HelperText, "Guidance")
            .Add(c => c.ErrorText, "Error")
            .Add(c => c.State, RefractionStates.Invalid)
            .Add(
                c => c.InputAttributes,
                new Dictionary<string, object>
                {
                    ["aria-describedby"] = " \tprivacy\rfield-helper field-error\fprivacy\n ",
                }));
        Assert.Equal("privacy field-helper field-error", cut.Find("input").GetAttribute("aria-describedby"));
        cut.Render(p => p.Add(
            c => c.InputAttributes,
            new Dictionary<string, object>
            {
                ["aria-describedby"] = "format\tprivacy format",
            }));
        Assert.Equal("format privacy field-helper field-error", cut.Find("input").GetAttribute("aria-describedby"));
    }

    /// <summary>Whitespace alone does not produce an empty description attribute.</summary>
    [Fact]
    public void WhitespaceDescriptionsAreOmitted()
    {
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p.Add(
            c => c.InputAttributes,
            new Dictionary<string, object>
            {
                ["aria-describedby"] = " \t\r\n\f ",
            }));
        Assert.False(cut.Find("input").HasAttribute("aria-describedby"));
    }
}