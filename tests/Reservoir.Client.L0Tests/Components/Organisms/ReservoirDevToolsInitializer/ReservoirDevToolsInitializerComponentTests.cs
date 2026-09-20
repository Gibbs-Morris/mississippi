using System.Reflection;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Client.Components.Organisms.ReservoirDevToolsInitializer;
using Mississippi.Reservoir.Core;

using Moq;


namespace Mississippi.Reservoir.Client.L0Tests.Components.Organisms.ReservoirDevToolsInitializer;

/// <summary>
///     Tests for <see cref="ReservoirDevToolsInitializerComponent" />.
/// </summary>
public sealed class ReservoirDevToolsInitializerComponentTests
{
    private static bool IsInitialized(
        ReduxDevToolsService service
    ) =>
        (bool)typeof(ReduxDevToolsService).GetField("isInitialized", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(service)!;

    /// <summary>
    ///     Rendering the component initializes DevTools and renderer disposal stops the service.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task RenderInitializesDevToolsAndDisposalStopsServiceAsync()
    {
        // Arrange
        ServiceCollection services = [];
        Mock<IJSRuntime> jsRuntime = new();
        ReservoirDevToolsInitializerTestHostCapture capture = new();
        services.AddSingleton(jsRuntime.Object);
        services.AddSingleton(capture);
        services.AddLogging();
        IReservoirBuilder builder = services.AddReservoir();
        builder.AddReservoirDevTools(options => options.Enablement = ReservoirDevToolsEnablement.Always);
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        DevToolsInitializationTracker tracker = serviceProvider.GetRequiredService<DevToolsInitializationTracker>();
        await using ReduxDevToolsService service = serviceProvider.GetRequiredService<ReduxDevToolsService>();

        // Act
        await using (HtmlRenderer renderer = new(serviceProvider, NullLoggerFactory.Instance))
        {
            _ = await renderer.Dispatcher.InvokeAsync(() =>
                renderer.RenderComponentAsync<ReservoirDevToolsInitializerTestHost>());
            ReservoirDevToolsInitializerComponent component = capture.Initializer!;
            MethodInfo onAfterRender = typeof(ReservoirDevToolsInitializerComponent).GetMethod(
                "OnAfterRender",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            onAfterRender.Invoke(component, [false]);

            // Assert - the renderer supplied the [Inject] service and the non-first-render path is a no-op.
            Assert.False(tracker.WasInitialized);
            Assert.False(IsInitialized(service));
            onAfterRender.Invoke(component, [true]);
            Assert.True(tracker.WasInitialized);
            Assert.True(IsInitialized(service));
        }

        Assert.False(IsInitialized(service));
    }
}