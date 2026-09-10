using System.Collections.Generic;

using Bunit;

using Mississippi.Refraction.Client.Components.Atoms.Input;


namespace Mississippi.Refraction.Client.L0Tests.Components.Atoms.Input;

/// <summary>Verifies typed caller attributes preserve valid ARIA values.</summary>
public sealed class InputFieldAriaValueTests : BunitContext
{
    /// <summary>Boolean description attributes never become references to True or False IDs.</summary>
    /// <param name="value">The native boolean attribute value.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BooleanDescriptionsDoNotCreateIds(
        bool value
    )
    {
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p.Add(c => c.Id, "field")
            .Add(c => c.HelperText, "Guidance")
            .Add(
                c => c.InputAttributes,
                new Dictionary<string, object>
                {
                    ["aria-describedby"] = value,
                }));
        Assert.Equal("field-helper", cut.Find("input").GetAttribute("aria-describedby"));
    }

    /// <summary>Caller invalidity values retain boolean omission and normalize known tokens.</summary>
    /// <param name="value">The caller-provided value.</param>
    /// <param name="expected">The resulting attribute value.</param>
    [Theory]
    [InlineData(false, null)]
    [InlineData(true, "true")]
    [InlineData(" TRUE ", "true")]
    [InlineData("FALSE", "false")]
    [InlineData("Grammar", "grammar")]
    [InlineData("SPELLING", "spelling")]
    [InlineData("custom", "custom")]
    [InlineData(1, "1")]
    public void CallerInvalidityValuesAreNormalized(
        object value,
        string? expected
    )
    {
        using IRenderedComponent<InputField> cut = Render<InputField>(p => p.Add(
            c => c.InputAttributes,
            new Dictionary<string, object>
            {
                ["ARIA-INVALID"] = value,
            }));
        Assert.Equal(expected, cut.Find("input").GetAttribute("aria-invalid"));
        cut.Render(p => p.Add(c => c.State, RefractionStates.Invalid));
        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
        cut.Render(p => p.Add(c => c.State, RefractionStates.Idle));
        Assert.Equal(expected, cut.Find("input").GetAttribute("aria-invalid"));
    }
}