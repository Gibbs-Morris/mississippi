using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.Commands;
using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.State;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Events;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Handlers;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Reducers;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.State;
using Mississippi.Tributary.Runtime;


namespace Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Registrations;

/// <summary>Registers shared membership behavior for application-defined aggregate and projection records.</summary>
/// <remarks>
///     Public as the reference-set feature boundary. Named handlers and reducers use framework dispatch and
///     immutability enforcement.
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
        services.AddSingleton(new ReferenceSetStateAccess<TAggregate, TSource>(read, write));
        services.AddCommandHandler<TAddCommand, TAggregate, AddReferenceHandler<TAddCommand, TAggregate, TSource>>();
        services
            .AddCommandHandler<TRemoveCommand, TAggregate,
                RemoveReferenceHandler<TRemoveCommand, TAggregate, TSource>>();
        services.AddReducer<ReferenceAdded, TAggregate, ReferenceAddedReducer<TAggregate, TSource>>();
        services.AddReducer<ReferenceRemoved, TAggregate, ReferenceRemovedReducer<TAggregate, TSource>>();
        return services.AddSnapshotStateConverter<TAggregate>();
    }

    /// <summary>Registers read-only membership replay for an application-defined projection.</summary>
    /// <typeparam name="TProjection">The named projection snapshot type.</typeparam>
    /// <typeparam name="TSource">The source projection type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="read">Reads membership from the projection.</param>
    /// <param name="write">Creates a new projection snapshot containing the supplied membership.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    ///     Callbacks must be pure. Use the collection's brook name and a distinct projection snapshot storage name.
    ///     This registration installs no command handlers or effects and performs no source reads.
    /// </remarks>
    public static IServiceCollection AddReferenceSetProjection<TProjection, TSource>(
        this IServiceCollection services,
        Func<TProjection?, ReferenceSetState<TSource>> read,
        Func<TProjection?, ReferenceSetState<TSource>, TProjection> write
    )
        where TProjection : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(read);
        ArgumentNullException.ThrowIfNull(write);
        services.AddEventType<ReferenceAdded>().AddEventType<ReferenceRemoved>();
        services.AddSnapshotType<TProjection>();
        services.AddSingleton(new ReferenceSetStateAccess<TProjection, TSource>(read, write));
        services.AddReducer<ReferenceAdded, TProjection, ReferenceAddedReducer<TProjection, TSource>>();
        services.AddReducer<ReferenceRemoved, TProjection, ReferenceRemovedReducer<TProjection, TSource>>();
        return services.AddSnapshotStateConverter<TProjection>();
    }
}