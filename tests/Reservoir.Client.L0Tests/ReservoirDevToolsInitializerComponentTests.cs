using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Core;

using Moq;


namespace Mississippi.Reservoir.Client.L0Tests;

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
    ///     The first render initializes DevTools and disposal stops the service.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task RenderInitializesDevToolsAndDisposalStopsServiceAsync()
    {
        // Arrange
        ServiceCollection services = [];
        Mock<IJSRuntime> jsRuntime = new();
        services.AddSingleton(jsRuntime.Object);
        services.AddLogging();
        IReservoirBuilder builder = services.AddReservoir();
        builder.AddReservoirDevTools(options => options.Enablement = ReservoirDevToolsEnablement.Always);
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        DevToolsInitializationTracker tracker = serviceProvider.GetRequiredService<DevToolsInitializationTracker>();
        await using ReduxDevToolsService service = serviceProvider.GetRequiredService<ReduxDevToolsService>();
        ReservoirDevToolsInitializerComponent component = new();
        PropertyInfo serviceProperty = typeof(ReservoirDevToolsInitializerComponent).GetProperty(
            "DevToolsService",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        serviceProperty.SetValue(component, service);
        MethodInfo onAfterRender = typeof(ReservoirDevToolsInitializerComponent).GetMethod(
            "OnAfterRender",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        // Act
        using (component)
        {
            onAfterRender.Invoke(component, [false]);
            onAfterRender.Invoke(component, [true]);

            // Assert
            Assert.True(tracker.WasInitialized);
            Assert.True(IsInitialized(service));
        }

        Assert.False(IsInitialized(service));
    }
}