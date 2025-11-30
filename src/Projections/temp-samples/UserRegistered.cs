using System;


namespace Mississippi.Projections.TempSamples;

/// <summary>
///     Event emitted when a user registers for the first time.
/// </summary>
/// <param name="UserId">The unique identifier assigned to the user.</param>
/// <param name="Email">The user's primary email address.</param>
/// <param name="DisplayName">The friendly display name initially chosen by the user.</param>
/// <param name="RegisteredAt">When the registration occurred.</param>
internal sealed record UserRegistered(
    Guid UserId,
    string Email,
    string DisplayName,
    DateTimeOffset RegisteredAt
) : IUserEvent;