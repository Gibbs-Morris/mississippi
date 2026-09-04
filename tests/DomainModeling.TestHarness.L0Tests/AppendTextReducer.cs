using System.Collections.Generic;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.DomainModeling.TestHarness.L0Tests;

/// <summary>
///     Appends text events to a new list so scenarios can verify replay order and state isolation.
/// </summary>
internal sealed class AppendTextReducer : EventReducerBase<string, List<string>>
{
    /// <inheritdoc />
    protected override List<string> ReduceCore(
        List<string> state,
        string eventData
    ) =>
        [.. state, eventData];
}