using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;


namespace Mississippi.Reservoir.Testing;

/// <summary>
///     A fluent test harness for testing Reservoir actions, reducers, and effects in isolation.
/// </summary>
/// <remarks>
///     <para>
///         This harness enables unit testing of Reservoir components without requiring
///         the full Store infrastructure. It supports Given/When/Then style scenarios
///         for testing state transitions and side effects.
///     </para>
///     <para>
///         <strong>Pattern Overview:</strong>
///     </para>
///     <list type="number">
///         <item>
///             <term>Given (state or actions)</term>
///             <description>Establish starting state directly or by applying actions.</description>
///         </item>
///         <item>
///             <term>When (action)</term>
///             <description>Dispatch an action to trigger reducers and effects.</description>
///         </item>
///         <item>
///             <term>Then (assertions)</term>
///             <description>Assert on resulting state and any actions emitted by effects.</description>
///         </item>
///     </list>
///     <para>
///         <strong>Example:</strong>
///     </para>
///     <code>
///         StoreTestHarnessFactory.ForFeature&lt;NavigationState&gt;()
///             .WithReducer&lt;LocationChangedAction&gt;(NavigationReducers.OnLocationChanged)
///             .CreateScenario()
///             .Given(new NavigationState())
///             .When(new LocationChangedAction("https://example.com/page", false))
///             .ThenState(s =&gt; s.CurrentUri.Should().Be("https://example.com/page"));
///     </code>
/// </remarks>
/// <typeparam name="TState">The feature state type being tested.</typeparam>
public sealed class StoreTestHarness<TState>
    where TState : class, IFeatureState, new()
{
    private readonly List<Func<IServiceProvider, IActionEffect<TState>>> effectFactories = [];

    private readonly List<Func<TState, IAction, TState>> reducers = [];

    private readonly IServiceCollection services = new ServiceCollection();

    /// <summary>
    ///     Gets the initial state for scenarios.
    /// </summary>
    internal TState InitialState { get; private set; } = new();

    /// <summary>
    ///     Creates a new scenario builder for Given/When/Then style testing.
    /// </summary>
    /// <returns>A new <see cref="StoreScenario{TState}" /> initialized with this harness's reducers and effects.</returns>
    public StoreScenario<TState> CreateScenario()
    {
        return new StoreScenario<TState>(reducers, effectFactories, InitialState, services);
    }

    /// <summary>
    ///     Registers an effect by type.
    /// </summary>
    /// <typeparam name="TEffect">The effect type implementing <see cref="IActionEffect{TState}" />.</typeparam>
    /// <returns>This harness for fluent chaining.</returns>
    public StoreTestHarness<TState> WithEffect<TEffect>()
        where TEffect : class, IActionEffect<TState>
    {
        services.AddTransient<TEffect>();
        effectFactories.Add(sp => sp.GetRequiredService<TEffect>());
        return this;
    }

    /// <summary>
    ///     Registers an effect instance directly.
    /// </summary>
    /// <param name="effect">The effect instance to register.</param>
    /// <returns>This harness for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="effect" /> is null.</exception>
    public StoreTestHarness<TState> WithEffect(
        IActionEffect<TState> effect
    )
    {
        ArgumentNullException.ThrowIfNull(effect);
        effectFactories.Add(_ => effect);
        return this;
    }

    /// <summary>
    ///     Sets the initial state for scenarios.
    /// </summary>
    /// <param name="state">The initial state to use.</param>
    /// <returns>This harness for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="state" /> is null.</exception>
    public StoreTestHarness<TState> WithInitialState(
        TState state
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        InitialState = state;
        return this;
    }

    /// <summary>
    ///     Registers a reducer as a delegate function.
    /// </summary>
    /// <typeparam name="TAction">The action type this reducer handles.</typeparam>
    /// <param name="reducer">The reducer function.</param>
    /// <returns>This harness for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="reducer" /> is null.</exception>
    public StoreTestHarness<TState> WithReducer<TAction>(
        Func<TState, TAction, TState> reducer
    )
        where TAction : IAction
    {
        ArgumentNullException.ThrowIfNull(reducer);
        reducers.Add((
            state,
            action
        ) => action is TAction typed ? reducer(state, typed) : state);
        return this;
    }

    /// <summary>
    ///     Registers a service for dependency injection into effects.
    /// </summary>
    /// <typeparam name="TService">The service interface type.</typeparam>
    /// <param name="instance">The service instance.</param>
    /// <returns>This harness for fluent chaining.</returns>
    public StoreTestHarness<TState> WithService<TService>(
        TService instance
    )
        where TService : class
    {
        services.AddSingleton(instance);
        return this;
    }
}