namespace Mississippi.DomainModeling.Runtime.L0Tests.EventEffects;

/// <summary>
///     Distinguishes lifecycle-test metric tags from other test aggregates.
/// </summary>
/// <param name="Count">The aggregate's current count.</param>
internal sealed record EffectLifecycleAggregate(int Count = 0);