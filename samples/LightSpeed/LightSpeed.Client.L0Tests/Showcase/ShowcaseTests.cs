using Microsoft.Extensions.DependencyInjection;

using Mississippi.Refraction.Client;
using Mississippi.Refraction.Client.Infrastructure.Theming;
using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Core;

using MississippiSamples.LightSpeed.Client.Features.Showcase;


namespace MississippiSamples.LightSpeed.Client.L0Tests.Showcase;

/// <summary>Verifies the sample's immutable state flow and selected form behavior.</summary>
public sealed class ShowcaseTests
{
    /// <summary>A corrected value clears validation feedback after submission.</summary>
    [Fact]
    public void EditingSubmittedValueRecomputesValidation()
    {
        ShowcaseState submitted = new()
        {
            Email = "invalid",
            IsSubmitted = true,
        };
        ShowcaseState edited = ShowcaseReducers.ChangeEmail(submitted, new("correct@example.com"));
        ShowcaseView view = ShowcaseSelectors.GetView(edited);
        Assert.Null(view.ErrorText);
        Assert.Equal(RefractionStates.Idle, view.InputState);
        Assert.Equal("Profile validated in this session.", view.StatusText);
    }

    /// <summary>Edits produce new state and retain the source value.</summary>
    [Fact]
    public void EditsAreImmutable()
    {
        ShowcaseState initial = new();
        ShowcaseState changed = ShowcaseReducers.ChangeEmail(initial, new("new@example.com"));
        Assert.NotSame(initial, changed);
        Assert.Equal("alex@contoso.example", initial.Email);
        Assert.Equal("new@example.com", changed.Email);
        Assert.Equal(1, changed.ActionCount);
        Assert.Equal(nameof(ChangeEmailAction), changed.LastAction);
    }

    /// <summary>The registered store dispatches actions through the real reducer pipeline.</summary>
    [Fact]
    public void RegistrationConnectsActionsToState()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddReservoir().AddShowcaseFeature();
        using ServiceProvider provider = services.BuildServiceProvider();
        IStore store = provider.GetRequiredService<IStore>();
        store.Dispatch(new ChangeThemeAction(RefractionThemeMode.HighContrast));
        store.Dispatch(new ChangeEmailAction("team@example.com"));
        store.Dispatch(new ValidateProfileAction());
        ShowcaseView view = ShowcaseSelectors.GetView(store.GetState<ShowcaseState>());
        Assert.Equal(RefractionThemeMode.HighContrast, view.ThemeMode);
        Assert.Equal("team@example.com", view.Email);
        Assert.Null(view.ErrorText);
        Assert.Equal(3, view.ActionCount);
        Assert.Equal(nameof(ValidateProfileAction), view.LastAction);
    }

    /// <summary>Reset preserves presentation preferences and increments the action trail.</summary>
    [Fact]
    public void ResetPreservesTheme()
    {
        ShowcaseState state = new()
        {
            ThemeMode = RefractionThemeMode.Light,
            Email = "edited",
            IsSubmitted = true,
            ActionCount = 4,
        };
        ShowcaseState reset = ShowcaseReducers.Reset(state, new());
        Assert.Equal(RefractionThemeMode.Light, reset.ThemeMode);
        Assert.Equal("alex@contoso.example", reset.Email);
        Assert.False(reset.IsSubmitted);
        Assert.Equal(5, reset.ActionCount);
        Assert.Equal(nameof(ResetProfileAction), reset.LastAction);
    }

    /// <summary>Invalid submitted values produce actionable feedback.</summary>
    /// <param name="email">An invalid address.</param>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-an-email")]
    public void SubmittedInvalidValuesShowFeedback(
        string email
    )
    {
        ShowcaseState state = new()
        {
            Email = email,
        };
        ShowcaseView view = ShowcaseSelectors.GetView(ShowcaseReducers.Validate(state, new()));
        Assert.Equal(RefractionStates.Invalid, view.InputState);
        Assert.False(string.IsNullOrWhiteSpace(view.ErrorText));
    }

    /// <summary>Unsupported theme values cannot reach the provider.</summary>
    [Fact]
    public void UnsupportedThemeIsIgnored()
    {
        ShowcaseState state = new();
        Assert.Same(state, ShowcaseReducers.ChangeTheme(state, new((RefractionThemeMode)99)));
    }
}