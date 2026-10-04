namespace Mississippi.DomainModeling.Runtime.L0Tests.EventEffects;

/// <summary>
///     Names the effect lifecycle boundaries exercised by the tests.
/// </summary>
internal enum EffectLifecyclePoint
{
    /// <summary>No operation fails.</summary>
    None,

    /// <summary>The effect creates its enumerable.</summary>
    Handle,

    /// <summary>The enumerable acquires its enumerator.</summary>
    Acquire,

    /// <summary>The enumerator advances.</summary>
    MoveNext,

    /// <summary>The enumerator reads its current item.</summary>
    Current,

    /// <summary>The enumerator releases its resources.</summary>
    Dispose,
}