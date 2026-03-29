using System;
using System.Threading.Tasks;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Refraction.Client.StateManagement.L0Tests.Scenes;

/// <summary>
///     Behavior tests for the renamed Reservoir scene base.
/// </summary>
public sealed class ReservoirSceneBaseBehaviorTests : BunitContext
{
    private static TestReservoirStore CreateStore(
        TestStoreSubscription subscription,
        TestReservoirFeatureState? state = null
    ) =>
        new(state ?? new TestReservoirFeatureState(), subscription);

    private IRenderedComponent<TestReservoirSceneComponent> RenderSceneComponent(
        IStore store
    )
    {
        Services.AddSingleton(store);
        return Render<TestReservoirSceneComponent>();
    }

    /// <summary>
    ///     ReservoirSceneBase defaults the loading and error flags to false.
    /// </summary>
    [Fact]
    public void ReservoirSceneBaseDefaultsHasErrorAndIsLoadingToFalse()
    {
        // Arrange
        using TestStoreSubscription subscription = new();
        using TestReservoirStore store = CreateStore(subscription);
        using IRenderedComponent<TestReservoirSceneComponent> cut = RenderSceneComponent(store);

        // Assert
        Assert.False(cut.Instance.ReadHasError());
        Assert.False(cut.Instance.ReadIsLoading());
    }

    /// <summary>
    ///     ReservoirSceneBase dispatches actions created by the parameterless helper.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ReservoirSceneBaseDispatchOnEventDispatchesCreatedAction()
    {
        // Arrange
        using TestStoreSubscription subscription = new();
        using TestReservoirStore store = CreateStore(subscription);
        using IRenderedComponent<TestReservoirSceneComponent> cut = RenderSceneComponent(store);
        Func<Task> handler = cut.Instance.CreateDispatchHandler(() => new TestReservoirSceneAction("apply"));

        // Act
        await handler();

        // Assert
        TestReservoirSceneAction action = Assert.IsType<TestReservoirSceneAction>(Assert.Single(store.DispatchedActions));
        Assert.Equal("apply", action.Value);
    }

    /// <summary>
    ///     ReservoirSceneBase dispatches actions created from event arguments.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ReservoirSceneBaseDispatchOnEventWithArgsDispatchesCreatedAction()
    {
        // Arrange
        using TestStoreSubscription subscription = new();
        using TestReservoirStore store = CreateStore(subscription);
        using IRenderedComponent<TestReservoirSceneComponent> cut = RenderSceneComponent(store);
        Func<string, Task> handler = cut.Instance.CreateDispatchHandler(value => new TestReservoirSceneAction(value));

        // Act
        await handler("dispatch");

        // Assert
        TestReservoirSceneAction action = Assert.IsType<TestReservoirSceneAction>(Assert.Single(store.DispatchedActions));
        Assert.Equal("dispatch", action.Value);
    }

    /// <summary>
    ///     ReservoirSceneBase exposes the current feature state through the protected State property.
    /// </summary>
    [Fact]
    public void ReservoirSceneBaseStateReturnsCurrentFeatureState()
    {
        // Arrange
        using TestStoreSubscription subscription = new();
        TestReservoirFeatureState expectedState = new()
        {
            Counter = 17,
        };
        using TestReservoirStore store = CreateStore(subscription, expectedState);
        using IRenderedComponent<TestReservoirSceneComponent> cut = RenderSceneComponent(store);

        // Act
        TestReservoirFeatureState actualState = cut.Instance.ReadState();

        // Assert
        Assert.Same(expectedState, actualState);
    }

    /// <summary>
    ///     ReservoirSceneBase replaces the existing subscription when the component is initialized again.
    /// </summary>
    [Fact]
    public void ReservoirSceneBaseReinitializeDisposesExistingSubscriptionBeforeResubscribing()
    {
        // Arrange
        using TestStoreSubscription firstSubscription = new();
        using TestStoreSubscription secondSubscription = new();
        using TestReservoirStore store = new(new TestReservoirFeatureState(), firstSubscription, secondSubscription);
        using IRenderedComponent<TestReservoirSceneComponent> cut = RenderSceneComponent(store);

        // Act
        cut.Instance.Reinitialize();

        // Assert
        Assert.Equal(1, firstSubscription.DisposeCallCount);
        Assert.Equal(2, store.SubscribeCallCount);
    }

    /// <summary>
    ///     ReservoirSceneBase subscribes during initialization and disposes the subscription exactly once.
    /// </summary>
    [Fact]
    public void ReservoirSceneBaseSubscribesOnInitializeAndDisposesSubscription()
    {
        // Arrange
        using TestStoreSubscription subscription = new();
        using TestReservoirStore store = CreateStore(subscription);
        using IRenderedComponent<TestReservoirSceneComponent> cut = RenderSceneComponent(store);

        // Assert
        Assert.Equal(1, store.SubscribeCallCount);

        // Act
        cut.Instance.Dispose();
        cut.Instance.Dispose();

        // Assert
        Assert.Equal(1, subscription.DisposeCallCount);
    }
}
