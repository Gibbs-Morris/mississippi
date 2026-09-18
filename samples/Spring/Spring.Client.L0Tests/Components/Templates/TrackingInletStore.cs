using System;
using System.Collections.Generic;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Refraction.Client.Infrastructure.Theming;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.Events;
using Mississippi.Reservoir.Abstractions.State;

using MississippiSamples.Spring.Client.Features.ThemePreferences;


namespace MississippiSamples.Spring.Client.L0Tests.Components.Templates;

/// <summary>
///     Tracks layout actions and subscriptions for L0 tests.
/// </summary>
internal sealed class TrackingInletStore : IInletStore
{
    private readonly List<IAction> actions = [];

    private readonly List<Action> listeners = [];

    private ThemePreferencesState themeState = new();

    /// <summary>Gets the actions dispatched through the store.</summary>
    public IReadOnlyList<IAction> Actions => actions;

    /// <summary>Gets the number of active listeners.</summary>
    public int ActiveSubscriptions => listeners.Count;

    /// <summary>Gets the empty store-event stream.</summary>
    public IObservable<StoreEventBase> StoreEvents => EmptyStoreEventObservable.Instance;

    /// <summary>Gets the selected theme mode.</summary>
    public RefractionThemeMode ThemeMode => themeState.ThemeMode;

    /// <summary>Dispatches an action and notifies listeners.</summary>
    /// <param name="action">The action to dispatch.</param>
    public void Dispatch(
        IAction action
    )
    {
        actions.Add(action);
        if (action is SetThemeModeAction setThemeModeAction && Enum.IsDefined(setThemeModeAction.Mode))
        {
            themeState = themeState with
            {
                ThemeMode = setThemeModeAction.Mode,
            };
        }

        foreach (Action listener in listeners.ToArray())
        {
            listener();
        }
    }

    /// <inheritdoc />
    public void Dispose() => listeners.Clear();

    /// <inheritdoc />
    public TState GetState<TState>()
        where TState : class, IFeatureState =>
        typeof(TState) == typeof(ThemePreferencesState)
            ? (TState)(object)themeState
            : throw new InvalidOperationException($"Unexpected state type {typeof(TState).Name}.");

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> GetStateSnapshot() =>
        new Dictionary<string, object>
        {
            [ThemePreferencesState.FeatureKey] = themeState,
        };

    /// <inheritdoc />
    public IDisposable Subscribe(
        Action listener
    )
    {
        listeners.Add(listener);
        return new TrackingStoreSubscription(listeners, listener);
    }
}