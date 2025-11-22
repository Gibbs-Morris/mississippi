using System;


namespace Mississippi.Projections.TempSamples;

/// <summary>
///     Event emitted when a user's email address changes.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="Email">The new email address.</param>
/// <param name="UpdatedAt">When the change occurred.</param>
internal sealed record UserEmailChanged(Guid UserId, string Email, DateTimeOffset UpdatedAt) : IUserEvent;