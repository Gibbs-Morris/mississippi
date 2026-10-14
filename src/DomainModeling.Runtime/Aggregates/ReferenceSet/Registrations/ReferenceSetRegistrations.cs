using System;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet;
using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.Commands;
using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.State;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Events;
using Mississippi.Tributary.Runtime;


namespace Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Registrations;

/// <summary>Registers shared membership behavior for application-defined aggregate and command records.</summary>
/// <remarks>
///     Public as the reference-set feature boundary. Existing delegate handlers and reducers enforce dispatch and
///     immutability.
/// </remarks>
public static class ReferenceSetRegistrations
{
    /// <summary>Registers membership decisions, replay, event identities and snapshot conversion.</summary>
    /// <typeparam name="TAggregate">The named aggregate snapshot type.</typeparam>
    /// <typeparam name="TSource">The source projection type.</typeparam>
    /// <typeparam name="TAddCommand">The concrete add command with declared properties.</typeparam>
    /// <typeparam name="TRemoveCommand">The concrete remove command with declared properties.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="read">Reads membership; a null aggregate represents an untouched stream.</param>
    /// <param name="write">Creates a new aggregate snapshot containing the supplied membership.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>Callbacks must be pure. Application existence and permission checks remain application policy.</remarks>
    public static IServiceCollection AddReferenceSet<TAggregate, TSource, TAddCommand, TRemoveCommand>(
        this IServiceCollection services,
        Func<TAggregate?, ReferenceSetState<TSource>> read,
        Func<TAggregate?, ReferenceSetState<TSource>, TAggregate> write
    )
        where TAggregate : class
        where TAddCommand : class, IAddReference<TSource>
        where TRemoveCommand : class, IRemoveReference<TSource>
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(read);
        ArgumentNullException.ThrowIfNull(write);
        services.AddEventType<ReferenceAdded>().AddEventType<ReferenceRemoved>();
        services.AddSnapshotType<TAggregate>();
        services.AddCommandHandler<TAddCommand, TAggregate>((command, state) => Add(command, state, read));
        services.AddCommandHandler<TRemoveCommand, TAggregate>((command, state) => Remove(command, state, read));
        services.AddReducer<ReferenceAdded, TAggregate>((state, added) => write(
            state,
            read(state) with
            {
                Members = read(state)
                    .Members.WithComparers(StringComparer.Ordinal)
                    .SetItem(added.SourceId, added.ObservedSourceVersion),
            }));
        services.AddReducer<ReferenceRemoved, TAggregate>((state, removed) => write(
            state,
            read(state) with
            {
                Members = read(state).Members.WithComparers(StringComparer.Ordinal).Remove(removed.SourceId),
            }));
        return services.AddSnapshotStateConverter<TAggregate>();
    }

    /// <summary>Decides whether a valid addition changes membership, retaining existing loading floors.</summary>
    /// <typeparam name="TAggregate">The named aggregate snapshot type.</typeparam>
    /// <typeparam name="TSource">The source projection type.</typeparam>
    /// <param name="command">The caller-supplied identifier and observed version.</param>
    /// <param name="state">The current snapshot, or null for an untouched stream.</param>
    /// <param name="read">The pure membership reader.</param>
    /// <returns>One recorded addition, a successful empty result, or a validation failure.</returns>
    private static OperationResult<IReadOnlyList<object>> Add<TAggregate, TSource>(
        IAddReference<TSource> command,
        TAggregate? state,
        Func<TAggregate?, ReferenceSetState<TSource>> read
    )
        where TAggregate : class
    {
        if (!IsValidId(command.SourceId) || (command.ObservedSourceVersion < 0))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Source ID must be nonblank and at most 256 UTF-16 code units; observed source version must be nonnegative.");
        }

        ReferenceSetState<TSource> references = read(state);
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

    /// <summary>Checks identifier bounds without changing its identity.</summary>
    /// <param name="sourceId">The caller-supplied source identifier.</param>
    /// <returns>Whether the identifier is nonblank and within the fixed UTF-16 length bound.</returns>
    private static bool IsValidId(
        string? sourceId
    ) =>
        !string.IsNullOrWhiteSpace(sourceId) && (sourceId.Length <= ReferenceSetLimits.MaximumSourceIdLength);

    /// <summary>Decides whether a valid removal changes membership.</summary>
    /// <typeparam name="TAggregate">The named aggregate snapshot type.</typeparam>
    /// <typeparam name="TSource">The source projection type.</typeparam>
    /// <param name="command">The caller-supplied identifier.</param>
    /// <param name="state">The current snapshot, or null for an untouched stream.</param>
    /// <param name="read">The pure membership reader.</param>
    /// <returns>One recorded removal, a successful empty result, or a validation failure.</returns>
    private static OperationResult<IReadOnlyList<object>> Remove<TAggregate, TSource>(
        IRemoveReference<TSource> command,
        TAggregate? state,
        Func<TAggregate?, ReferenceSetState<TSource>> read
    )
        where TAggregate : class
    {
        if (!IsValidId(command.SourceId))
        {
            return OperationResult.Fail<IReadOnlyList<object>>(
                AggregateErrorCodes.InvalidCommand,
                "Source ID must be nonblank and at most 256 UTF-16 code units.");
        }

        return OperationResult.Ok<IReadOnlyList<object>>(
            read(state).Members.WithComparers(StringComparer.Ordinal).ContainsKey(command.SourceId)
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