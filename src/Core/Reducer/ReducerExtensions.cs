using Microsoft.Extensions.DependencyInjection;


namespace Mississippi.Core.Reducer;

/// <summary>
///     Extension methods for registering reducer services.
/// </summary>
public static class ReducerExtensions
{
    /// <summary>
    ///     Registers <typeparamref name="TReducer" /> as the singleton implementation of
    ///     <see cref="IReducer{TState}" /> in the supplied <paramref name="services" />.
    /// </summary>
    /// <typeparam name="TState">Aggregate-root state type.</typeparam>
    /// <typeparam name="TReducer">Concrete reducer.</typeparam>
    /// <param name="services">The DI service collection.</param>
    /// <returns>The same collection for fluent chaining.</returns>
    public static IServiceCollection AddReducer<TState, TReducer>(
        this IServiceCollection services
    )
        where TReducer : class, IReducer<TState>
    {
        services.AddSingleton<IReducer<TState>, TReducer>();
        return services;
    }

    /// <summary>
    ///     Registers the open-generic mapping <c>IRootReducer&lt;T&gt; → RootReducer&lt;T&gt;</c>
    ///     so any state type can be resolved without an explicit binding.
    /// </summary>
    /// <param name="services">The DI service collection.</param>
    /// <returns>The same collection for fluent chaining.</returns>
    public static IServiceCollection AddRootReducer(
        this IServiceCollection services
    )
    {
        services.AddSingleton(typeof(IRootReducer<>), typeof(RootReducer<>));
        return services;
    }
}