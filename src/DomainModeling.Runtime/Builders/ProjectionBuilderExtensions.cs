using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.Abstractions.Builders;
using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime;


namespace Mississippi.DomainModeling.Runtime.Builders;

/// <summary>
///     Extension methods for registering projection components through the builder model.
/// </summary>
public static class ProjectionBuilderExtensions
{
    /// <summary>
    ///     Registers UX projection infrastructure and marks the projection type as registered.
    /// </summary>
    /// <typeparam name="TProjection">The projection state type.</typeparam>
    /// <param name="builder">The projection builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IProjectionBuilder AddProjection<TProjection>(
        this IProjectionBuilder builder
    )
    {
        ProjectionBuilder impl = CastBuilder(builder);
        impl.EnsureNotDuplicate<TProjection>();
        IServiceCollection services = impl.Services;
        services.TryAddSingleton<IUxProjectionGrainFactory, UxProjectionGrainFactory>();
        return builder;
    }

    /// <summary>
    ///     Registers an event reducer for projection state computation.
    /// </summary>
    /// <typeparam name="TEvent">The event type consumed by the reducer.</typeparam>
    /// <typeparam name="TProjection">The projection state type.</typeparam>
    /// <typeparam name="TReducer">The reducer implementation type.</typeparam>
    /// <param name="builder">The projection builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IProjectionBuilder AddReducer<TEvent, TProjection, TReducer>(
        this IProjectionBuilder builder
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
    ///     Registers a snapshot state converter for periodic projection state snapshots.
    /// </summary>
    /// <typeparam name="TSnapshot">The projection state type to convert.</typeparam>
    /// <param name="builder">The projection builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IProjectionBuilder AddSnapshotStateConverter<TSnapshot>(
        this IProjectionBuilder builder
    )
    {
        IServiceCollection services = CastBuilder(builder).Services;
        services.TryAddTransient<ISnapshotStateConverter<TSnapshot>, SnapshotStateConverter<TSnapshot>>();
        return builder;
    }

    private static ProjectionBuilder CastBuilder(
        IProjectionBuilder builder
    ) =>
        builder as ProjectionBuilder ??
        throw new InvalidOperationException(
            $"Expected {nameof(ProjectionBuilder)} but received {builder.GetType().FullName}.");
}