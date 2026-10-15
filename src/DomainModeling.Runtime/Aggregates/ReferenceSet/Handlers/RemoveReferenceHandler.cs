using System;
using System.Collections.Generic;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet;
using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.Commands;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Events;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.State;


namespace Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Handlers;

/// <summary>Validates removals and records a fact only for existing membership.</summary>
/// <typeparam name="TCommand">The concrete source-typed remove command.</typeparam>
/// <typeparam name="TAggregate">The named aggregate snapshot type.</typeparam>
/// <typeparam name="TSource">The source projection type.</typeparam>
internal sealed class RemoveReferenceHandler<TCommand, TAggregate, TSource> : CommandHandlerBase<TCommand, TAggregate>
    where TCommand : class, IRemoveReference<TSource>
    where TAggregate : class
{
    /// <summary>Initializes a new instance of the <see cref="RemoveReferenceHandler{TCommand, TAggregate, TSource}" /> class.</summary>
    /// <param name="stateAccess">The pure snapshot callbacks.</param>
    public RemoveReferenceHandler(
        ReferenceSetStateAccess<TAggregate, TSource> stateAccess
    ) =>
        StateAccess = stateAccess;

    private ReferenceSetStateAccess<TAggregate, TSource> StateAccess { get; }

    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        TCommand command,
        TAggregate? state
    )
    {
        if (string.IsNullOrWhiteSpace(command.SourceId) ||
            (command.SourceId.Length > ReferenceSetLimits.MaximumSourceIdLength))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Source ID must be nonblank and at most 256 UTF-16 code units.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            StateAccess.Read(state).Members.WithComparers(StringComparer.Ordinal).ContainsKey(command.SourceId)
                ?
                [
                    new ReferenceRemoved
                    {
                        SourceId = command.SourceId,
                    },
                ]
                : []);
    }
}