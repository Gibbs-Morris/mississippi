using System;


namespace Mississippi.Projections.TempSamples;

/// <summary>
///     Contract implemented by all user domain events utilized in the reducer samples.
/// </summary>
internal interface IUserEvent
{
    /// <summary>
    ///     Gets the unique user identifier targeted by the event.
    /// </summary>
    Guid UserId { get; }
}