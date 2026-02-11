using System;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.Builders;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Builders;

/// <summary>
///     Builder for Reservoir feature registration.
/// </summary>
/// <typeparam name="TState">The feature state type.</typeparam>
public sealed class ReservoirFeatureBuilder<TState> : IReservoirFeatureBuilder<TState>
    where TState : class, IFeatureState, new()
{
    private IReservoirBuilder Parent { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReservoirFeatureBuilder{TState}" /> class.
    /// </summary>
    /// <param name="parent">The parent builder.</param>
    public ReservoirFeatureBuilder(
        IReservoirBuilder parent
    )
    {
        ArgumentNullException.ThrowIfNull(parent);
        Parent = parent;
    }

    /// <inheritdoc />
    public IReservoirFeatureBuilder<TState> AddActionEffect<TEffect>()
        where TEffect : class, IActionEffect<TState>
    {
        Parent.ConfigureServices(services => services.AddTransient<IActionEffect<TState>, TEffect>());
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
        Parent.ConfigureServices(services =>
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
        Parent.ConfigureServices(services =>
        {
            services.AddTransient<IActionReducer<TState>, TReducer>();
            services.AddTransient<IActionReducer<TAction, TState>, TReducer>();
        });
        AddRootReducer();
        return this;
    }

    private void AddRootActionEffect()
    {
        Parent.ConfigureServices(services =>
            services.TryAddTransient<IRootActionEffect<TState>, RootActionEffect<TState>>());
    }

    private void AddRootReducer()
    {
        Parent.ConfigureServices(services => services.TryAddTransient<IRootReducer<TState>, RootReducer<TState>>());
    }
}