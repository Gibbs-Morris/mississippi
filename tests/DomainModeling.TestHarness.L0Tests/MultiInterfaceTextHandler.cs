using System;
using System.Collections.Generic;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.TestHarness.L0Tests;

/// <summary>
///     Exposes unrelated generic interfaces to verify that scenario dispatch selects command contracts.
/// </summary>
internal sealed class MultiInterfaceTextHandler
    : IComparable<string>,
      IComparable<Uri>,
      ICommandHandler<string, List<string>>
{
    private TextCommandHandler Handler { get; } = new();

    /// <inheritdoc />
    public OperationResult<IReadOnlyList<object>> Handle(
        string command,
        List<string>? state
    ) =>
        Handler.Handle(command, state);

    /// <inheritdoc />
    public bool TryHandle(
        object command,
        List<string>? state,
        out OperationResult<IReadOnlyList<object>> result
    ) =>
        Handler.TryHandle(command, state, out result);

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