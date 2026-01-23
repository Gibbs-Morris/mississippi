using System.Collections.Generic;


namespace Mississippi.Refraction.Events;

/// <summary>
///     Event raised when navigation is requested.
/// </summary>
/// <param name="Target">The navigation target (scene id, route, etc.).</param>
/// <param name="Parameters">Optional navigation parameters.</param>
public sealed record NavigationRequestedEvent(string Target, IReadOnlyDictionary<string, object>? Parameters = null);