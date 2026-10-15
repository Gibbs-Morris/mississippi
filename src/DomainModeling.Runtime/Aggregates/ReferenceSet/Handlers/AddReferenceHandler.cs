using System;
using System.Collections.Generic;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet;
using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.Commands;
using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.State;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Events;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.State;


namespace Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Handlers;

/// <summary>Validates additions and retains the original loading floor for existing membership.</summary>
/// <typeparam name="TCommand">The concrete source-typed add command.</typeparam>
/// <typeparam name="TAggregate">The named aggregate snapshot type.</typeparam>
/// <typeparam name="TSource">The source projection type.</typeparam>
internal sealed class AddReferenceHandler<TCommand, TAggregate, TSource> : CommandHandlerBase<TCommand, TAggregate>
    where TCommand : class, IAddReference<TSource>
    where TAggregate : class
{
    /// <summary>Initializes a new instance of the <see cref="AddReferenceHandler{TCommand, TAggregate, TSource}" /> class.</summary>
    /// <param name="stateAccess">The pure snapshot callbacks.</param>
    public AddReferenceHandler(
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
            (command.SourceId.Length > ReferenceSetLimits.MaximumSourceIdLength) ||
            (command.ObservedSourceVersion < 0))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Source ID must be nonblank and at most 256 UTF-16 code units; observed source version must be nonnegative.");
        }

        ReferenceSetState<TSource> references = StateAccess.Read(state);
        if (references.Members.WithComparers(StringComparer.Ordinal).ContainsKey(command.SourceId))
        {
            return OperationResult.Ok<IReadOnlyList<object>>([]);
        }

        if (references.Members.Count >= ReferenceSetLimits.MaximumMembers)
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidState,
                "The reference set has reached its 1000-member capacity.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
        [
            new ReferenceAdded
            {
                SourceId = command.SourceId,
                ObservedSourceVersion = command.ObservedSourceVersion,
            },
        ]);
    }
}