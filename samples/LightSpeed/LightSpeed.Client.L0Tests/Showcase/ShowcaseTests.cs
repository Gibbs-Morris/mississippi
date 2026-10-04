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

    /// <summary>Emitter activation is immutable and records the latest action.</summary>
    [Fact]
    public void EmitterActivationIsImmutable()
    {
        ShowcaseState initial = new();
        ShowcaseState changed = ShowcaseReducers.ActivateEmitter(initial, new());
        Assert.NotSame(initial, changed);
        Assert.Equal(0, initial.EmitterActivationCount);
        Assert.Equal(1, changed.EmitterActivationCount);
        Assert.Equal(1, changed.ActionCount);
        Assert.Equal(nameof(ActivateEmitterAction), changed.LastAction);
    }

    /// <summary>Emitter disabled intent is immutable and updates the selected state.</summary>
    [Fact]
    public void EmitterDisabledChangesAreImmutable()
    {
        ShowcaseState initial = new();
        ShowcaseState changed = ShowcaseReducers.ChangeEmitterDisabled(initial, new(true));
        Assert.NotSame(initial, changed);
        Assert.False(initial.IsEmitterDisabled);
        Assert.True(changed.IsEmitterDisabled);
        Assert.Equal(1, changed.ActionCount);
        Assert.Equal(nameof(ChangeEmitterDisabledAction), changed.LastAction);
    }

    /// <summary>The registered emitter actions reach the selector view.</summary>
    [Fact]
    public void EmitterRegistrationConnectsActionsToState()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddReservoir().AddShowcaseFeature();
        using ServiceProvider provider = services.BuildServiceProvider();
        IStore store = provider.GetRequiredService<IStore>();
        store.Dispatch(new ActivateEmitterAction());
        store.Dispatch(new ChangeEmitterDisabledAction(true));
        ShowcaseView view = ShowcaseSelectors.GetView(store.GetState<ShowcaseState>());
        Assert.Equal(1, view.EmitterActivationCount);
        Assert.True(view.IsEmitterDisabled);
        Assert.Equal(2, view.ActionCount);
        Assert.Equal(nameof(ChangeEmitterDisabledAction), view.LastAction);
    }

    /// <summary>Invalid demo percentages do not produce misleading state.</summary>
    /// <param name="percent">An out-of-range selection.</param>
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void InvalidProgressPreservesState(
        int percent
    )
    {
        ShowcaseState state = new();
        Assert.Same(state, ShowcaseReducers.ChangeProgress(state, new(percent)));
    }

    /// <summary>The registered notification intents reach the selected view.</summary>
    [Fact]
    public void NotificationRegistrationConnectsActionsToState()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddReservoir().AddShowcaseFeature();
        using ServiceProvider provider = services.BuildServiceProvider();
        IStore store = provider.GetRequiredService<IStore>();
        store.Dispatch(new ExpandNotificationAction());
        store.Dispatch(new DismissNotificationAction());
        store.Dispatch(new RestoreNotificationAction());
        ShowcaseView view = ShowcaseSelectors.GetView(store.GetState<ShowcaseState>());
        Assert.True(view.IsNotificationVisible);
        Assert.False(view.IsNotificationExpanded);
        Assert.Equal(3, view.ActionCount);
        Assert.Equal(nameof(RestoreNotificationAction), view.LastAction);
    }

    /// <summary>Notification intents preserve immutable state and reject invalid visibility transitions.</summary>
    [Fact]
    public void NotificationTransitionsAreImmutable()
    {
        ShowcaseState initial = new();
        ShowcaseState expanded = ShowcaseReducers.ExpandNotification(initial, new());
        ShowcaseState dismissed = ShowcaseReducers.DismissNotification(expanded, new());
        ShowcaseState ignoredDismissal = ShowcaseReducers.DismissNotification(dismissed, new());
        ShowcaseState ignoredExpansion = ShowcaseReducers.ExpandNotification(dismissed, new());
        ShowcaseState restored = ShowcaseReducers.RestoreNotification(dismissed, new());
        ShowcaseState ignoredRestore = ShowcaseReducers.RestoreNotification(restored, new());
        Assert.NotSame(initial, expanded);
        Assert.True(initial.IsNotificationVisible);
        Assert.False(initial.IsNotificationExpanded);
        Assert.True(expanded.IsNotificationVisible);
        Assert.True(expanded.IsNotificationExpanded);
        Assert.Equal(nameof(ExpandNotificationAction), expanded.LastAction);
        Assert.Equal(1, expanded.ActionCount);
        Assert.False(dismissed.IsNotificationVisible);
        Assert.False(dismissed.IsNotificationExpanded);
        Assert.Equal(nameof(DismissNotificationAction), dismissed.LastAction);
        Assert.Equal(2, dismissed.ActionCount);
        Assert.Same(dismissed, ignoredDismissal);
        Assert.Equal(nameof(DismissNotificationAction), ignoredDismissal.LastAction);
        Assert.Equal(2, ignoredDismissal.ActionCount);
        Assert.Same(dismissed, ignoredExpansion);
        Assert.True(restored.IsNotificationVisible);
        Assert.False(restored.IsNotificationExpanded);
        Assert.Equal(nameof(RestoreNotificationAction), restored.LastAction);
        Assert.Equal(3, restored.ActionCount);
        Assert.Same(restored, ignoredRestore);
    }

    /// <summary>Completion changes preserve the original state and unrelated form data.</summary>
    [Fact]
    public void ProgressChangesAreImmutable()
    {
        ShowcaseState state = new()
        {
            Email = "custom@example.com",
            IsSubmitted = true,
        };
        ShowcaseState changed = ShowcaseReducers.ChangeProgress(state, new(null));
        Assert.Equal(25, state.ProgressPercent);
        Assert.Null(changed.ProgressPercent);
        Assert.Equal(state.Email, changed.Email);
        Assert.True(changed.IsSubmitted);
        Assert.Equal(1, changed.ActionCount);
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
            EmitterActivationCount = 2,
            IsEmitterDisabled = true,
            IsNotificationVisible = true,
            IsNotificationExpanded = true,
        };
        ShowcaseState reset = ShowcaseReducers.Reset(state, new());
        Assert.Equal(RefractionThemeMode.Light, reset.ThemeMode);
        Assert.Equal("alex@contoso.example", reset.Email);
        Assert.False(reset.IsSubmitted);
        Assert.Equal(2, reset.EmitterActivationCount);
        Assert.True(reset.IsEmitterDisabled);
        Assert.True(reset.IsNotificationVisible);
        Assert.True(reset.IsNotificationExpanded);
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