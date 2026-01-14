using Mississippi.Reservoir.Abstractions.State;


namespace Cascade.Client.Chat;

/// <summary>
///     Represents the chat feature state for Redux-style state management.
/// </summary>
/// <remarks>
///     Note: Available channels and messages come from projections subscribed via InletStore,
///     not from this feature state. This state tracks authentication and UI concerns only.
/// </remarks>
internal sealed record ChatState : IFeatureState
{
    /// <summary>
    ///     Gets the unique key identifying this feature state in the store.
    /// </summary>
    public static string FeatureKey => "chat";

    /// <summary>
    ///     Gets the error message if login failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the user is authenticated.
    /// </summary>
    public bool IsAuthenticated { get; init; }

    /// <summary>
    ///     Gets a value indicating whether a login attempt is in progress.
    /// </summary>
    public bool IsLoggingIn { get; init; }

    /// <summary>
    ///     Gets the currently selected channel ID.
    /// </summary>
    public string? SelectedChannelId { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the create channel modal is open.
    /// </summary>
    public bool ShowCreateChannelModal { get; init; }

    /// <summary>
    ///     Gets the current user's display name.
    /// </summary>
    public string UserDisplayName { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the current user's ID.
    /// </summary>
    public string UserId { get; init; } = string.Empty;
}