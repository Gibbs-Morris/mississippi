using System.ComponentModel.DataAnnotations;

using Mississippi.Refraction.Client;


namespace MississippiSamples.LightSpeed.Client.Features.Showcase;

/// <summary>Derives the UI contract from local state.</summary>
internal static class ShowcaseSelectors
{
    private static EmailAddressAttribute EmailValidator { get; } = new();

    /// <summary>Selects the values needed by the presentational components.</summary>
    /// <param name="state">The feature state.</param>
    /// <returns>The derived presentation data.</returns>
    public static ShowcaseView GetView(
        ShowcaseState state
    )
    {
        string? error = null;
        if (state.IsSubmitted)
        {
            if (string.IsNullOrWhiteSpace(state.Email))
            {
                error = "Enter your work email address.";
            }
            else if (!EmailValidator.IsValid(state.Email))
            {
                error = "Use a complete email address.";
            }
        }

        string status = state.IsSubmitted && error is null
            ? "Profile validated in this session."
            : "Edits stay in this demo session.";
        return new(
            state.ThemeMode,
            state.Email,
            error is null ? RefractionStates.Idle : RefractionStates.Invalid,
            error,
            status,
            state.ActionCount,
            state.LastAction);
    }
}