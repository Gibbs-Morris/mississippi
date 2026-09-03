using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Client.BuiltIn.Components;
using Mississippi.Reservoir.Client.BuiltIn.Navigation;
using Mississippi.Reservoir.Client.BuiltIn.Navigation.State;
using Mississippi.Reservoir.Core;


namespace Mississippi.Reservoir.Client.L0Tests.BuiltIn.Components;

/// <summary>
///     Tests for <see cref="ReservoirNavigationProvider" />.
/// </summary>
public sealed class ReservoirNavigationProviderTests : IDisposable
{
    private readonly TestableNavigationManager navigationManager;

    private readonly ServiceProvider serviceProvider;

    private readonly IStore store;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReservoirNavigationProviderTests" /> class.
    /// </summary>
    public ReservoirNavigationProviderTests()
    {
        ServiceCollection services = [];
        navigationManager = new(uri: "https://example.com/start");
        services.AddSingleton<NavigationManager>(navigationManager);
        IReservoirBuilder builder = services.AddReservoir();
        builder.AddBuiltInNavigation();
        serviceProvider = services.BuildServiceProvider();
        store = serviceProvider.GetRequiredService<IStore>();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        store.Dispose();
        serviceProvider.Dispose();
    }

    /// <summary>
    ///     Initialization and navigation changes should dispatch location actions.
    /// </summary>
    /// <returns>A task representing the test operation.</returns>
    [Fact(DisplayName = "Initialization And Navigation Changes Dispatch Location Actions")]
    public async Task InitializationAndNavigationChangesShouldDispatchLocationActions()
    {
        // Arrange
        NavigationState state;

        // Act
        using (HtmlRenderer renderer = new(serviceProvider, NullLoggerFactory.Instance))
        {
            await renderer.Dispatcher.InvokeAsync(async () =>
                await renderer.RenderComponentAsync<ReservoirNavigationProvider>());
            navigationManager.TriggerLocationChanged(true);
            state = store.GetState<NavigationState>();
        }

        // Assert
        Assert.Equal("https://example.com/start", state.CurrentUri);
        Assert.Equal(2, state.NavigationCount);
        Assert.True(state.IsNavigationIntercepted);
        navigationManager.TriggerLocationChanged(false);
        state = store.GetState<NavigationState>();
        Assert.Equal(2, state.NavigationCount);
    }
}
