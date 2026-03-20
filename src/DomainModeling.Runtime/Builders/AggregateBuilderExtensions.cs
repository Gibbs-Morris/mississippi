using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Brooks.Abstractions;
using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Abstractions.Builders;
using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime;


namespace Mississippi.DomainModeling.Runtime.Builders;

/// <summary>
///     Extension methods for registering aggregate components through the builder model.
/// </summary>
public static class AggregateBuilderExtensions
{
    /// <summary>
    ///     Registers aggregate infrastructure and marks the aggregate type as registered.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate state type.</typeparam>
    /// <param name="builder">The aggregate builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IAggregateBuilder AddAggregate<TAggregate>(
        this IAggregateBuilder builder
    )
        where TAggregate : class
    {
        AggregateBuilder impl = CastBuilder(builder);
        impl.EnsureNotDuplicate<TAggregate>();
        IServiceCollection services = impl.Services;
        services.TryAddSingleton<IEventTypeRegistry>(provider =>
        {
            EventTypeRegistry registry = new();
            foreach (IEventTypeRegistration registration in provider.GetServices<IEventTypeRegistration>())
            {
                registration.Register(registry);
            }

            return registry;
        });
        services.TryAddSingleton<ISnapshotTypeRegistry>(provider =>
        {
            SnapshotTypeRegistry registry = new();
            foreach (ISnapshotTypeRegistration registration in provider.GetServices<ISnapshotTypeRegistration>())
            {
                registration.Register(registry);
            }

            return registry;
        });
        services.TryAddTransient<IBrookEventConverter, BrookEventConverter>();
        services.TryAddTransient<IAggregateGrainFactory, AggregateGrainFactory>();
        services.TryAddSingleton(TimeProvider.System);
        return builder;
    }

    /// <summary>
    ///     Registers a command handler for processing commands against aggregate state.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TSnapshot">The aggregate state type.</typeparam>
    /// <typeparam name="THandler">The command handler implementation type.</typeparam>
    /// <param name="builder">The aggregate builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IAggregateBuilder AddCommandHandler<TCommand, TSnapshot, THandler>(
        this IAggregateBuilder builder
    )
        where THandler : class, ICommandHandler<TCommand, TSnapshot>
    {
        IServiceCollection services = CastBuilder(builder).Services;
        services.AddTransient<ICommandHandler<TSnapshot>, THandler>();
        services.AddTransient<ICommandHandler<TCommand, TSnapshot>, THandler>();
        services.TryAddTransient<IRootCommandHandler<TSnapshot>, RootCommandHandler<TSnapshot>>();
        return builder;
    }

    /// <summary>
    ///     Registers an event effect for processing events on a specific aggregate type.
    /// </summary>
    /// <typeparam name="TEffect">The effect implementation type.</typeparam>
    /// <typeparam name="TAggregate">The aggregate state type this effect handles.</typeparam>
    /// <param name="builder">The aggregate builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IAggregateBuilder AddEventEffect<TEffect, TAggregate>(
        this IAggregateBuilder builder
    )
        where TEffect : class, IEventEffect<TAggregate>
        where TAggregate : class
    {
        IServiceCollection services = CastBuilder(builder).Services;
        services.AddTransient<IEventEffect<TAggregate>, TEffect>();
        services.TryAddTransient<IRootEventEffect<TAggregate>, RootEventEffect<TAggregate>>();
        return builder;
    }

    /// <summary>
    ///     Registers an event type so it can be resolved during aggregate hydration.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="builder">The aggregate builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IAggregateBuilder AddEventType<TEvent>(
        this IAggregateBuilder builder
    )
        where TEvent : class
    {
        IServiceCollection services = CastBuilder(builder).Services;
        services.AddSingleton<IEventTypeRegistration>(new EventTypeRegistration<TEvent>());
        return builder;
    }

    /// <summary>
    ///     Registers a fire-and-forget event effect for processing events on a specific aggregate type.
    /// </summary>
    /// <typeparam name="TEffect">The effect implementation type.</typeparam>
    /// <typeparam name="TEvent">The event type this effect handles.</typeparam>
    /// <typeparam name="TAggregate">The aggregate state type.</typeparam>
    /// <param name="builder">The aggregate builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IAggregateBuilder AddFireAndForgetEventEffect<TEffect, TEvent, TAggregate>(
        this IAggregateBuilder builder
    )
        where TEffect : class, IFireAndForgetEventEffect<TEvent, TAggregate>
        where TEvent : class
        where TAggregate : class
    {
        IServiceCollection services = CastBuilder(builder).Services;
        services.AddTransient<IFireAndForgetEventEffect<TEvent, TAggregate>, TEffect>();
        services.AddSingleton<IFireAndForgetEffectRegistration<TAggregate>>(
            new FireAndForgetEffectRegistration<TEffect, TEvent, TAggregate>());
        return builder;
    }

    /// <summary>
    ///     Registers an event reducer for state computation.
    /// </summary>
    /// <typeparam name="TEvent">The event type consumed by the reducer.</typeparam>
    /// <typeparam name="TProjection">The projection/aggregate state type.</typeparam>
    /// <typeparam name="TReducer">The reducer implementation type.</typeparam>
    /// <param name="builder">The aggregate builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IAggregateBuilder AddReducer<TEvent, TProjection, TReducer>(
        this IAggregateBuilder builder
    )
        where TReducer : class, IEventReducer<TEvent, TProjection>
    {
        IServiceCollection services = CastBuilder(builder).Services;
        services.AddTransient<IEventReducer<TProjection>, TReducer>();
        services.AddTransient<IEventReducer<TEvent, TProjection>, TReducer>();
        services.TryAddTransient<IRootReducer<TProjection>, RootReducer<TProjection>>();
        return builder;
    }

    /// <summary>
    ///     Registers a snapshot state converter for periodic aggregate state snapshots.
    /// </summary>
    /// <typeparam name="TSnapshot">The state type to convert.</typeparam>
    /// <param name="builder">The aggregate builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IAggregateBuilder AddSnapshotStateConverter<TSnapshot>(
        this IAggregateBuilder builder
    )
    {
        IServiceCollection services = CastBuilder(builder).Services;
        services.TryAddTransient<ISnapshotStateConverter<TSnapshot>, SnapshotStateConverter<TSnapshot>>();
        return builder;
    }

    /// <summary>
    ///     Registers a snapshot type so it can be resolved during snapshot loading.
    /// </summary>
    /// <typeparam name="TSnapshot">The snapshot type.</typeparam>
    /// <param name="builder">The aggregate builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IAggregateBuilder AddSnapshotType<TSnapshot>(
        this IAggregateBuilder builder
    )
        where TSnapshot : class
    {
        IServiceCollection services = CastBuilder(builder).Services;
        services.AddSingleton<ISnapshotTypeRegistration>(new SnapshotTypeRegistration<TSnapshot>());
        return builder;
    }

    private static AggregateBuilder CastBuilder(
        IAggregateBuilder builder
    ) =>
        builder as AggregateBuilder ??
        throw new InvalidOperationException(
            $"Expected {nameof(AggregateBuilder)} but received {builder.GetType().FullName}.");
}