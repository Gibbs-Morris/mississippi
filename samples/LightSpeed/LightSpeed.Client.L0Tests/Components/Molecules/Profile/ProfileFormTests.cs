using Bunit;

using Mississippi.Refraction.Client;
using Mississippi.Refraction.Client.Infrastructure.Theming;

using MississippiSamples.LightSpeed.Client.Components.Molecules.Profile;
using MississippiSamples.LightSpeed.Client.Features.Showcase;


namespace MississippiSamples.LightSpeed.Client.L0Tests.Components.Molecules.Profile;

/// <summary>Verifies the form's controlled values and intent callbacks.</summary>
public sealed class ProfileFormTests : BunitContext
{
    private static ShowcaseView InitialView =>
        new(RefractionThemeMode.Dark, "team@example.com", RefractionStates.Idle, null, "Ready", 0, "Ready");

    /// <summary>Editing emits intent without mutating the supplied presentation data.</summary>
    [Fact]
    public void EmailEditEmitsIntent()
    {
        string? edited = null;
        using IRenderedComponent<ProfileForm> cut = Render<ProfileForm>(p => p
            .Add(c => c.View, InitialView)
            .Add(c => c.EmailChanged, value => edited = value));
        cut.Find("input").Input("new@example.com");
        Assert.Equal("new@example.com", edited);
        Assert.Equal("team@example.com", cut.Instance.View.Email);
    }

    /// <summary>The parent can replace validation feedback and the displayed value.</summary>
    [Fact]
    public void ParentUpdatesRenderedFeedback()
    {
        using IRenderedComponent<ProfileForm> cut = Render<ProfileForm>(p => p.Add(c => c.View, InitialView));
        cut.Render(p => p.Add(
            c => c.View,
            InitialView with
            {
                Email = "invalid",
                InputState = RefractionStates.Invalid,
                ErrorText = "Use a complete email address.",
            }));
        Assert.Equal("invalid", cut.Find("input").GetAttribute("value"));
        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
        Assert.Equal("Use a complete email address.", cut.Find("[role=alert]").TextContent);
        cut.Render(p => p.Add(c => c.View, InitialView));
        Assert.Empty(cut.FindAll("[role=alert]"));
    }

    /// <summary>Submit and reset invoke only their corresponding callbacks.</summary>
    [Fact]
    public void SubmitAndResetEmitSeparateIntents()
    {
        int submits = 0;
        int resets = 0;
        using IRenderedComponent<ProfileForm> cut = Render<ProfileForm>(p => p
            .Add(c => c.View, InitialView)
            .Add(c => c.Validate, () => submits++)
            .Add(c => c.Reset, () => resets++));
        cut.Find("form").Submit();
        Assert.Equal(1, submits);
        Assert.Equal(0, resets);
        cut.Find("button[type=button]").Click();
        Assert.Equal(1, submits);
        Assert.Equal(1, resets);
    }
}