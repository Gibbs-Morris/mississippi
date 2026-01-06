using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Ripples.Abstractions;
using Mississippi.Ripples.Abstractions.Actions;
using Mississippi.Ripples.Abstractions.State;


namespace Mississippi.Ripples.L0Tests;

/// <summary>
///     Tests for <see cref="RippleRegistrations" />.
/// </summary>
[AllureParentSuite("Mississippi.Ripples")]
[AllureSuite("DI")]
[AllureSubSuite("RippleRegistrations")]
public sealed class RippleRegistrationsTests
{
    private sealed record SetPanelAction(string Panel) : IAction;

    private sealed class SetPanelReducer : ActionReducer<SetPanelAction, TestSidebarState>
    {
        protected override TestSidebarState ReduceCore(
            TestSidebarState state,
            SetPanelAction action
        ) =>
            state with
            {
                Panel = action.Panel,
            };
    }

    private sealed class TestEffect : IEffect
    {
        public bool CanHandle(
            IAction action
        ) =>
            false;

#pragma warning disable CS1998 // Async method lacks 'await' operators - test stub
        public async IAsyncEnumerable<IAction> HandleAsync(
            IAction action,
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
#pragma warning restore CS1998 // Async method lacks 'await' operators - test stub
        {
            yield break;
        }
    }

    private sealed class TestMiddleware : IMiddleware
    {
        public void Invoke(
            IAction action,
            Action<IAction> dispatch
        ) =>
            dispatch(action);
    }

    private sealed record TestSidebarState : IFeatureState
    {
        public static string FeatureKey => "sidebar";

        public bool IsOpen { get; init; }

        public string Panel { get; init; } = string.Empty;
    }

    private sealed record ToggleSidebarAction : IAction;

    private sealed class ToggleSidebarReducer : ActionReducer<ToggleSidebarAction, TestSidebarState>
    {
        protected override TestSidebarState ReduceCore(
            TestSidebarState state,
            ToggleSidebarAction action
        ) =>
            state with
            {
                IsOpen = !state.IsOpen,
            };
    }

    /// <summary>
    ///     Verifies that AddEffect registers the effect.
    /// </summary>
    [Fact]
    [AllureFeature("AddEffect")]
    public void AddEffectRegistersEffect()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddEffect<TestEffect>();
        using ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IEffect? effect = provider.GetService<IEffect>();
        Assert.NotNull(effect);
        Assert.IsType<TestEffect>(effect);
    }

    /// <summary>
    ///     Verifies that AddMiddleware registers the middleware.
    /// </summary>
    [Fact]
    [AllureFeature("AddMiddleware")]
    public void AddMiddlewareRegistersMiddleware()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddMiddleware<TestMiddleware>();
        using ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IMiddleware? middleware = provider.GetService<IMiddleware>();
        Assert.NotNull(middleware);
        Assert.IsType<TestMiddleware>(middleware);
    }

    /// <summary>
    ///     Verifies that AddReducer with class registers the reducer and root action reducer.
    /// </summary>
    [Fact]
    [AllureFeature("AddReducer")]
    public void AddReducerWithClassRegistersReducerAndRootActionReducer()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddReducer<ToggleSidebarAction, TestSidebarState, ToggleSidebarReducer>();
        using ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IActionReducer<TestSidebarState>? reducer = provider.GetService<IActionReducer<TestSidebarState>>();
        IRootActionReducer<TestSidebarState>? rootReducer = provider.GetService<IRootActionReducer<TestSidebarState>>();
        Assert.NotNull(reducer);
        Assert.IsType<ToggleSidebarReducer>(reducer);
        Assert.NotNull(rootReducer);
        Assert.IsType<RootActionReducer<TestSidebarState>>(rootReducer);
    }

    /// <summary>
    ///     Verifies that AddReducer with delegate registers the reducer and root action reducer.
    /// </summary>
    [Fact]
    [AllureFeature("AddReducer")]
    public void AddReducerWithDelegateRegistersReducerAndRootActionReducer()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddReducer<ToggleSidebarAction, TestSidebarState>((
            state,
            _
        ) => state with
        {
            IsOpen = !state.IsOpen,
        });
        using ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IActionReducer<TestSidebarState>? reducer = provider.GetService<IActionReducer<TestSidebarState>>();
        IRootActionReducer<TestSidebarState>? rootReducer = provider.GetService<IRootActionReducer<TestSidebarState>>();
        Assert.NotNull(reducer);
        Assert.NotNull(rootReducer);
    }

    /// <summary>
    ///     Verifies that AddRippleStore registers IRippleStore.
    /// </summary>
    [Fact]
    [AllureFeature("AddRippleStore")]
    public void AddRippleStoreRegistersIRippleStore()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddRippleStore();
        using ServiceProvider root = services.BuildServiceProvider();
        using IServiceScope scope = root.CreateScope();

        // Assert
        IRippleStore? store = scope.ServiceProvider.GetService<IRippleStore>();
        Assert.NotNull(store);
    }

    /// <summary>
    ///     Verifies that multiple reducers for the same state can be registered.
    /// </summary>
    [Fact]
    [AllureFeature("AddReducer")]
    public void MultipleReducersForSameStateCanBeRegistered()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddReducer<ToggleSidebarAction, TestSidebarState, ToggleSidebarReducer>();
        services.AddReducer<SetPanelAction, TestSidebarState, SetPanelReducer>();
        using ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IActionReducer<TestSidebarState>[]
            reducers = provider.GetServices<IActionReducer<TestSidebarState>>().ToArray();
        IRootActionReducer<TestSidebarState>? rootReducer = provider.GetService<IRootActionReducer<TestSidebarState>>();
        Assert.Equal(2, reducers.Length);
        Assert.NotNull(rootReducer);
    }

    /// <summary>
    ///     Verifies that RootActionReducer receives all registered reducers via DI.
    /// </summary>
    [Fact]
    [AllureFeature("Integration")]
    public void RootActionReducerReceivesAllRegisteredReducersViaDI()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddReducer<ToggleSidebarAction, TestSidebarState, ToggleSidebarReducer>();
        services.AddReducer<SetPanelAction, TestSidebarState, SetPanelReducer>();
        using ServiceProvider provider = services.BuildServiceProvider();

        // Act
        IRootActionReducer<TestSidebarState>? rootReducer = provider.GetService<IRootActionReducer<TestSidebarState>>();
        TestSidebarState initialState = new()
        {
            IsOpen = false,
            Panel = "default",
        };
        TestSidebarState afterToggle = rootReducer!.Reduce(initialState, new ToggleSidebarAction());
        TestSidebarState afterSetPanel = rootReducer.Reduce(afterToggle, new SetPanelAction("channels"));

        // Assert
        Assert.True(afterToggle.IsOpen);
        Assert.Equal("default", afterToggle.Panel);
        Assert.True(afterSetPanel.IsOpen);
        Assert.Equal("channels", afterSetPanel.Panel);
    }
}