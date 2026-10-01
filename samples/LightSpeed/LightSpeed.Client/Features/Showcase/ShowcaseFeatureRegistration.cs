using Mississippi.Reservoir.Abstractions;


namespace MississippiSamples.LightSpeed.Client.Features.Showcase;

/// <summary>Registers the client-only showcase state and reducers.</summary>
internal static class ShowcaseFeatureRegistration
{
    /// <summary>Adds the showcase feature.</summary>
    /// <param name="builder">The Reservoir builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IReservoirBuilder AddShowcaseFeature(
        this IReservoirBuilder builder
    )
    {
        builder.AddFeatureState<ShowcaseState>(feature => feature
            .AddReducer<ActivateEmitterAction>(ShowcaseReducers.ActivateEmitter)
            .AddReducer<ChangeEmailAction>(ShowcaseReducers.ChangeEmail)
            .AddReducer<ChangeEmitterDisabledAction>(ShowcaseReducers.ChangeEmitterDisabled)
            .AddReducer<ChangeProgressAction>(ShowcaseReducers.ChangeProgress)
            .AddReducer<ChangeThemeAction>(ShowcaseReducers.ChangeTheme)
            .AddReducer<ValidateProfileAction>(ShowcaseReducers.Validate)
            .AddReducer<ResetProfileAction>(ShowcaseReducers.Reset));
        return builder;
    }
}