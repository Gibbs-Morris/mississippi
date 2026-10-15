using System;

using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.State;


namespace Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.State;

/// <summary>Supplies pure application snapshot callbacks to the named membership implementations.</summary>
/// <typeparam name="TAggregate">The named aggregate snapshot type.</typeparam>
/// <typeparam name="TSource">The source projection type.</typeparam>
internal sealed class ReferenceSetStateAccess<TAggregate, TSource>
    where TAggregate : class
{
    /// <summary>Initializes a new instance of the <see cref="ReferenceSetStateAccess{TAggregate, TSource}" /> class.</summary>
    /// <param name="read">The pure membership reader.</param>
    /// <param name="write">The pure immutable snapshot writer.</param>
    public ReferenceSetStateAccess(
        Func<TAggregate?, ReferenceSetState<TSource>> read,
        Func<TAggregate?, ReferenceSetState<TSource>, TAggregate> write
    )
    {
        Read = read;
        Write = write;
    }

    /// <summary>Gets the callback that reads membership from a snapshot or untouched stream.</summary>
    public Func<TAggregate?, ReferenceSetState<TSource>> Read { get; }

    /// <summary>Gets the callback that creates a new named snapshot containing membership.</summary>
    public Func<TAggregate?, ReferenceSetState<TSource>, TAggregate> Write { get; }
}