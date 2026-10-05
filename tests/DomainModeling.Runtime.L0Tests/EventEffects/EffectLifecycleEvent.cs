namespace Mississippi.DomainModeling.Runtime.L0Tests.EventEffects;

/// <summary>
///     Event handled by the lifecycle-test effects.
/// </summary>
/// <param name="Value">The event's payload.</param>
internal sealed record EffectLifecycleEvent(string Value = "lifecycle");