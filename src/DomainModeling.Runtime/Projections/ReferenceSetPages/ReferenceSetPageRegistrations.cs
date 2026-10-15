using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.State;
using Mississippi.DomainModeling.Abstractions.Projections.ReferenceSetPages;
using Mississippi.DomainModeling.Runtime.Aggregates.ReferenceSet.Registrations;


namespace Mississippi.DomainModeling.Runtime.Projections.ReferenceSetPages;

/// <summary>Registers complete opt-in membership replay and bounded reads for named projections.</summary>
/// <remarks>Public as the consumer registration boundary; ordinary projection contracts remain unchanged.</remarks>
public static class ReferenceSetPageRegistrations
{
    /// <summary>Composes existing read-only membership registration with its typed bounded reader.</summary>
    /// <typeparam name="TProjection">The named membership projection with a public parameterless constructor.</typeparam>
    /// <typeparam name="TSource">The referenced source projection type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="read">The pure membership accessor.</param>
    /// <param name="write">The pure immutable projection composition callback.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddReferenceSetProjectionPages<TProjection, TSource>(
        this IServiceCollection services,
        Func<TProjection?, ReferenceSetState<TSource>> read,
        Func<TProjection?, ReferenceSetState<TSource>, TProjection> write
    )
        where TProjection : class, new()
    {
        services.AddReferenceSetProjection(read, write);
        services
            .TryAddSingleton<IReferenceSetPageReader<TProjection, TSource>,
                ReferenceSetPageReader<TProjection, TSource>>();
        return services;
    }
}