using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.Builders;
using Mississippi.Reservoir.Abstractions.State;
using Mississippi.Reservoir.State;


namespace Mississippi.Reservoir.Builders;

/// <summary>
///     Builder for Reservoir feature registration.
/// </summary>
/// <typeparam name="TState">The feature state type.</typeparam>
public sealed class ReservoirFeatureBuilder<TState> : IReservoirFeatureBuilder<TState>
    where TState : class, IFeatureState, new()
{
    private readonly IReservoirBuilder parent;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReservoirFeatureBuilder{TState}" /> class.
    /// </summary>
    /// <param name="parent">The parent builder.</param>
    public ReservoirFeatureBuilder(
        IReservoirBuilder parent
    )
    {
        ArgumentNullException.ThrowIfNull(parent);
        this.parent = parent;
        AddFeatureState();
    }

    /// <inheritdoc />
    public IReservoirFeatureBuilder<TState> AddActionEffect<TEffect>()
        where TEffect : class, IActionEffect<TState>
    {
        parent.ConfigureServices(services => services.AddTransient<IActionEffect<TState>, TEffect>());
        AddRootActionEffect();
        return this;
    }

    /// <inheritdoc />
    public IReservoirFeatureBuilder<TState> AddReducer<TAction>(
        Func<TState, TAction, TState> reduce
    )
        where TAction : IAction
    {
        ArgumentNullException.ThrowIfNull(reduce);
        parent.ConfigureServices(services =>
        {
            services.AddTransient<DelegateActionReducer<TAction, TState>>(_ => new(reduce));
            services.AddTransient<IActionReducer<TState>>(sp =>
                sp.GetRequiredService<DelegateActionReducer<TAction, TState>>());
            services.AddTransient<IActionReducer<TAction, TState>>(sp =>
                sp.GetRequiredService<DelegateActionReducer<TAction, TState>>());
        });
        AddRootReducer();
        return this;
    }

    /// <inheritdoc />
    public IReservoirFeatureBuilder<TState> AddReducer<TAction, TReducer>()
        where TAction : IAction
        where TReducer : class, IActionReducer<TAction, TState>
    {
        parent.ConfigureServices(services =>
        {
            services.AddTransient<IActionReducer<TState>, TReducer>();
            services.AddTransient<IActionReducer<TAction, TState>, TReducer>();
        });
        AddRootReducer();
        return this;
    }

    /// <inheritdoc />
    public IReservoirBuilder Done() => parent;

    private void AddFeatureState()
    {
        parent.ConfigureServices(services =>
            services.TryAddEnumerable(
                ServiceDescriptor.Scoped<IFeatureStateRegistration, FeatureStateRegistration<TState>>(sp => new(
                    sp.GetService<IRootReducer<TState>>(),
                    sp.GetService<IRootActionEffect<TState>>()))));
    }

    private void AddRootActionEffect()
    {
        parent.ConfigureServices(services =>
            services.TryAddTransient<IRootActionEffect<TState>, RootActionEffect<TState>>());
    }

    private void AddRootReducer()
    {
        parent.ConfigureServices(services =>
            services.TryAddTransient<IRootReducer<TState>, RootReducer<TState>>());
    }
}
