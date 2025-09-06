using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Core.Abstractions.Mapping;

/// <summary>
///     Provides extension methods for registering mappers in the dependency injection container.
/// </summary>
public static class MappingRegistrations
{
    /// <summary>
    ///     Adds Projection mapper to the service collection.
    /// </summary>
    /// <typeparam name="TFrom">The type of the source objects.</typeparam>
    /// <typeparam name="TTo">The type of the target objects.</typeparam>
    /// <typeparam name="TMapper">The type of the mapper.</typeparam>
    /// <param name="services">The service collection to add the mapper to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddMapper<TFrom, TTo, TMapper>(
        this IServiceCollection services
    )
        where TMapper : class, IMapper<TFrom, TTo>
    {
        services.AddTransient<IMapper<TFrom, TTo>, TMapper>();
        return services;
    }

    /// <summary>
    ///     Adds an IEnumerable mapper to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the mapper to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddIEnumerableMapper(
        this IServiceCollection services
    )
    {
        services.AddTransient(typeof(IEnumerableMapper<,>), typeof(EnumerableMapper<,>));
        return services;
    }

    /// <summary>
    ///     Adds an IAsyncEnumerable mapper to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add the mapper to.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddIAsyncEnumerableMapper(
        this IServiceCollection services
    )
    {
        services.AddTransient(typeof(IAsyncEnumerableMapper<,>), typeof(AsyncEnumerableMapper<,>));
        return services;
    }
}