using System;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Represents one shared source-state read for latest-state replica fan-out.
/// </summary>
internal sealed class ReplicaSinkSourceState
{
    private ReplicaSinkSourceState(
        ReplicaSinkSourceStateKind kind,
        long sourcePosition,
        object? value
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sourcePosition);
        if ((kind == ReplicaSinkSourceStateKind.Value) && (value is null))
        {
            throw new ArgumentNullException(nameof(value));
        }

        Kind = kind;
        SourcePosition = sourcePosition;
        Value = value;
    }

    /// <summary>
    ///     Gets the source-state semantics.
    /// </summary>
    public ReplicaSinkSourceStateKind Kind { get; }

    /// <summary>
    ///     Gets the source position represented by this read.
    /// </summary>
    public long SourcePosition { get; }

    /// <summary>
    ///     Gets the concrete source value when <see cref="Kind" /> is <see cref="ReplicaSinkSourceStateKind.Value" />.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    ///     Creates an absent-state source read.
    /// </summary>
    /// <param name="sourcePosition">The source position represented by the read.</param>
    /// <returns>The absent-state source read.</returns>
    public static ReplicaSinkSourceState Absent(
        long sourcePosition
    ) =>
        new(ReplicaSinkSourceStateKind.Absent, sourcePosition, null);

    /// <summary>
    ///     Creates a deleted-state source read.
    /// </summary>
    /// <param name="sourcePosition">The source position represented by the read.</param>
    /// <returns>The deleted-state source read.</returns>
    public static ReplicaSinkSourceState Deleted(
        long sourcePosition
    ) =>
        new(ReplicaSinkSourceStateKind.Deleted, sourcePosition, null);

    /// <summary>
    ///     Creates a value-state source read.
    /// </summary>
    /// <param name="sourcePosition">The source position represented by the read.</param>
    /// <param name="value">The concrete source value.</param>
    /// <returns>The value-state source read.</returns>
    public static ReplicaSinkSourceState FromValue(
        long sourcePosition,
        object value
    ) =>
        new(ReplicaSinkSourceStateKind.Value, sourcePosition, value);
}
