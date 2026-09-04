using System;
using System.Collections.Generic;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.DomainModeling.TestHarness.L0Tests;

/// <summary>
///     Exposes unrelated generic interfaces to verify that replay selects reducer contracts.
/// </summary>
internal sealed class MultiInterfaceTextReducer
    : IComparable<string>,
      IComparable<Uri>,
      IEventReducer<string, List<string>>
{
    private AppendTextReducer Reducer { get; } = new();

    /// <inheritdoc />
    public List<string> Reduce(
        List<string> state,
        string eventData
    ) =>
        Reducer.Reduce(state, eventData);

    /// <inheritdoc />
    public bool TryReduce(
        List<string> state,
        object eventData,
        out List<string> projection
    ) =>
        Reducer.TryReduce(state, eventData, out projection);

    /// <inheritdoc />
    int IComparable<string>.CompareTo(
        string? other
    ) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    int IComparable<Uri>.CompareTo(
        Uri? other
    ) =>
        throw new NotSupportedException();
}