using System;
using System.Collections.Generic;
using System.Linq;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client.Abstractions;
using Mississippi.Inlet.Client.SignalRConnection;
using Mississippi.Refraction.Client.Infrastructure.Theming;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.Events;
using Mississippi.Reservoir.Abstractions.State;

using MississippiSamples.Spring.Client.Features.ThemePreferences;


namespace MississippiSamples.Spring.Client.L0Tests.Components.Templates;

/// <summary>
///     Tests the Spring layout's store integration.
/// </summary>
public sealed class MainLayoutTests : BunitContext
{
    private sealed class EmptyObservable : IObservable<StoreEventBase>
    {
        public static EmptyObservable Instance { get; } = new();

        public IDisposable Subscribe(
            IObserver<StoreEventBase> observer
        ) =>
            new EmptySubscription();
    }

    private sealed class EmptySubscription : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private sealed class TrackingInletStore : IInletStore
    {
        private readonly List<IAction> actions = [];

        private readonly List<Action> listeners = [];

        private ThemePreferencesState themeState = new();

        public IReadOnlyList<IAction> Actions => actions;

        public int ActiveSubscriptions => listeners.Count;

        public IObservable<StoreEventBase> StoreEvents => EmptyObservable.Instance;

        public RefractionThemeMode ThemeMode => themeState.ThemeMode;

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

        public void Dispose() => listeners.Clear();

        public TState GetState<TState>()
            where TState : class, IFeatureState =>
            typeof(TState) == typeof(ThemePreferencesState)
                ? (TState)(object)themeState
                : throw new InvalidOperationException($"Unexpected state type {typeof(TState).Name}.");

        public IReadOnlyDictionary<string, object> GetStateSnapshot() =>
            new Dictionary<string, object>
            {
                [ThemePreferencesState.FeatureKey] = themeState,
            };

        public IDisposable Subscribe(
            Action listener
        )
        {
            listeners.Add(listener);
            return new StoreSubscription(listeners, listener);
        }

        private sealed class StoreSubscription : IDisposable
        {
            private readonly List<Action> listeners;

            private Action? listener;

            public StoreSubscription(
                List<Action> listeners,
                Action listener
            )
            {
                this.listeners = listeners;
                this.listener = listener;
            }

            public void Dispose()
            {
                if (listener is not null)
                {
                    listeners.Remove(listener);
                    listener = null;
                }
            }
        }
    }

    /// <summary>
    ///     The layout dispatches connection setup, rerenders from theme state, and disposes its subscription.
    /// </summary>
    [Fact]
    public void LayoutIntegratesWithThemeStore()
    {
        using TrackingInletStore store = new();
        Services.AddSingleton<IInletStore>(store);
        using (IRenderedComponent<MainLayout> cut = Render<MainLayout>())
        {
            Assert.Contains(store.Actions, action => action is RequestSignalRConnectionAction);
            Assert.Equal(1, store.ActiveSubscriptions);
            Assert.Equal("dark", cut.Find("[data-rf-theme]").GetAttribute("data-rf-theme"));
            cut.FindAll("button")
                .Single(button => button.TextContent.Contains("Light", StringComparison.Ordinal))
                .Click();
            Assert.Contains(store.Actions, action => action is SetThemeModeAction { Mode: RefractionThemeMode.Light });
            Assert.Equal(RefractionThemeMode.Light, store.ThemeMode);
            Assert.Equal("light", cut.Find("[data-rf-theme]").GetAttribute("data-rf-theme"));
            cut.Instance.Dispose();
        }

        Assert.Equal(0, store.ActiveSubscriptions);
    }
}