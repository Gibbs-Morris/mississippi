namespace Cascade.Components.ViewModels;

/// <summary>
///     View model for displaying a channel member in the UI.
/// </summary>
public sealed record MemberViewModel
{
    /// <summary>
    ///     Gets the display name of the member.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the member is online.
    /// </summary>
    public bool IsOnline { get; init; }

    /// <summary>
    ///     Gets the user ID of the member.
    /// </summary>
    public required string UserId { get; init; }
}