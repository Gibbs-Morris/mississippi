using System;


namespace Mississippi.Projections.TempSamples;

/// <summary>
///     Event emitted when a user is deactivated.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="Reason">The reason for the deactivation.</param>
/// <param name="DeactivatedAt">When the deactivation took effect.</param>
internal sealed record UserDeactivated(Guid UserId, string Reason, DateTimeOffset DeactivatedAt) : IUserEvent;