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
    /// <param name="services">The service collection.</param>
    /// <param name="parent">The parent builder.</param>
    public ReservoirFeatureBuilder(
        IServiceCollection services,
        IReservoirBuilder parent
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(parent);
        Services = services;
        this.parent = parent;
        AddFeatureState();
    }

    private IServiceCollection Services { get; }

    /// <inheritdoc />
    public IReservoirFeatureBuilder<TState> AddActionEffect<TEffect>()
        where TEffect : class, IActionEffect<TState>
    {
        Services.AddTransient<IActionEffect<TState>, TEffect>();
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
        Services.AddTransient<DelegateActionReducer<TAction, TState>>(_ => new(reduce));
        Services.AddTransient<IActionReducer<TState>>(sp =>
            sp.GetRequiredService<DelegateActionReducer<TAction, TState>>());
        Services.AddTransient<IActionReducer<TAction, TState>>(sp =>
            sp.GetRequiredService<DelegateActionReducer<TAction, TState>>());
        AddRootReducer();
        return this;
    }

    /// <inheritdoc />
    public IReservoirFeatureBuilder<TState> AddReducer<TAction, TReducer>()
        where TAction : IAction
        where TReducer : class, IActionReducer<TAction, TState>
    {
        Services.AddTransient<IActionReducer<TState>, TReducer>();
        Services.AddTransient<IActionReducer<TAction, TState>, TReducer>();
        AddRootReducer();
        return this;
    }

    /// <inheritdoc />
    public IReservoirBuilder Done() => parent;

    private void AddFeatureState()
    {
        Services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IFeatureStateRegistration, FeatureStateRegistration<TState>>(sp => new(
                sp.GetService<IRootReducer<TState>>(),
                sp.GetService<IRootActionEffect<TState>>())));
    }

    private void AddRootActionEffect()
    {
        Services.TryAddTransient<IRootActionEffect<TState>, RootActionEffect<TState>>();
    }

    private void AddRootReducer()
    {
        Services.TryAddTransient<IRootReducer<TState>, RootReducer<TState>>();
    }
}
