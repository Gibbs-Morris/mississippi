using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Core;


namespace MississippiTests.Refraction.Client.StateManagement.L0Tests.Scenes;

/// <summary>
///     Verifies scene rendering with the real store and Blazor dispatcher.
/// </summary>
public sealed class SceneBaseRenderingTests
{
    private static ServiceCollection CreateServices()
    {
        ServiceCollection services = [];
        services.AddReservoir()
            .AddFeatureState<SceneRenderingState>(feature =>
                feature.AddReducer<IncrementSceneCounterAction, SceneRenderingReducer>());
        return services;
    }

    /// <summary>
    ///     A serialized background dispatch works before any scene subscribes.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task BackgroundDispatchWithoutSceneUpdatesStateAndNotifiesListenersAsync()
    {
        // Arrange
        ServiceCollection services = CreateServices();
        await using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IStore store = scope.ServiceProvider.GetRequiredService<IStore>();
        int listenerCalls = 0;
        using IDisposable subscription = store.Subscribe(() => listenerCalls++);

        // Act
        await Task.Run(() => store.Dispatch(new IncrementSceneCounterAction()), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, store.GetState<SceneRenderingState>().Counter);
        Assert.Equal(1, listenerCalls);
    }

    /// <summary>
    ///     A scene renders serialized updates both on and outside its dispatcher.
    /// </summary>
    /// <param name="isBackgroundDispatch">Whether the producer runs outside Blazor's dispatcher.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StateUpdateRendersSceneAndNotifiesLaterListenersAsync(
        bool isBackgroundDispatch
    )
    {
        // Arrange
        ServiceCollection services = CreateServices();
        await using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IStore store = scope.ServiceProvider.GetRequiredService<IStore>();
        await using HtmlRenderer renderer = new(scope.ServiceProvider, NullLoggerFactory.Instance);
        HtmlRootComponent component = await renderer.Dispatcher.InvokeAsync(() =>
            renderer.RenderComponentAsync<CounterScene>());
        string initialHtml = await renderer.Dispatcher.InvokeAsync(component.ToHtmlString);
        Assert.Equal("<output>0</output>", initialHtml);
        int laterListenerCalls = 0;
        using IDisposable laterSubscription = store.Subscribe(() => laterListenerCalls++);

        // Act
        if (isBackgroundDispatch)
        {
            await Task.Run(
                () =>
                {
                    Assert.False(renderer.Dispatcher.CheckAccess());
                    store.Dispatch(new IncrementSceneCounterAction());
                },
                TestContext.Current.CancellationToken);
        }
        else
        {
            await renderer.Dispatcher.InvokeAsync(() =>
            {
                Assert.True(renderer.Dispatcher.CheckAccess());
                store.Dispatch(new IncrementSceneCounterAction());
            });
        }

        // Reading through the dispatcher also waits for the queued render to complete.
        string updatedHtml = await renderer.Dispatcher.InvokeAsync(component.ToHtmlString);

        // Assert
        Assert.Equal(1, store.GetState<SceneRenderingState>().Counter);
        Assert.Equal(1, laterListenerCalls);
        Assert.Equal("<output>1</output>", updatedHtml);
    }
}