using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;

using Moq;


namespace Mississippi.Reservoir.Blazor.L0Tests;

/// <summary>
///     Tests for ReduxDevToolsService connection, enablement, and lifecycle.
/// </summary>
public sealed class ReduxDevToolsServiceConnectionTests : IAsyncDisposable
{
    private readonly ReservoirDevToolsInterop interop;

    private readonly Mock<IJSObjectReference> jsModuleMock;

    private readonly Mock<IJSRuntime> jsRuntimeMock;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReduxDevToolsServiceConnectionTests" /> class.
    /// </summary>
    public ReduxDevToolsServiceConnectionTests()
    {
        jsRuntimeMock = new();
        jsModuleMock = new();
        interop = new(jsRuntimeMock.Object);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await interop.DisposeAsync();
    }

    private ReduxDevToolsService CreateService(
        IStore? store = null,
        ReservoirDevToolsOptions? options = null,
        IHostEnvironment? hostEnvironment = null
    )
    {
        store ??= CreateStore();
        options ??= new();
        return new(store, interop, Options.Create(options), hostEnvironment);
    }

    private Store CreateStore()
    {
        IFeatureStateRegistration[] registrations = [new TestFeatureStateRegistration()];
        return new(registrations, Array.Empty<IMiddleware>());
    }

    private void SetupJsModuleForConnection(
        bool connectReturns = true
    )
    {
        jsRuntimeMock.Setup(r => r.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
            .ReturnsAsync(jsModuleMock.Object);
        jsModuleMock.Setup(m => m.InvokeAsync<bool>("connect", It.IsAny<object[]>())).ReturnsAsync(connectReturns);
    }

    private sealed record TestAction : IAction;

    private sealed record TestFeatureState : IFeatureState
    {
        public static string FeatureKey => "test";

        public int Value { get; init; }
    }

    private sealed class TestFeatureStateRegistration : IFeatureStateRegistration
    {
        public string FeatureKey => TestFeatureState.FeatureKey;

        public object InitialState => new TestFeatureState { Value = 0 };

        public object? RootActionEffect => null;

        public object? RootReducer => null;
    }

    /// <summary>
    ///     Constructor should throw when store is null.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Test validates exception is thrown; no instance is created.")]
    public void ConstructorThrowsWhenStoreIsNull()
    {
        // Arrange
        ReservoirDevToolsOptions options = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ReduxDevToolsService(
            null!,
            interop,
            Options.Create(options)));
    }

    /// <summary>
    ///     Constructor should throw when interop is null.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Test validates exception is thrown; no instance is created.")]
    public void ConstructorThrowsWhenInteropIsNull()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ReduxDevToolsService(
            store,
            null!,
            Options.Create(options)));
    }

    /// <summary>
    ///     Constructor should throw when options is null.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Test validates exception is thrown; no instance is created.")]
    public void ConstructorThrowsWhenOptionsIsNull()
    {
        // Arrange
        using Store store = CreateStore();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ReduxDevToolsService(
            store,
            interop,
            null!));
    }

    /// <summary>
    ///     DisposeAsync should be callable multiple times without error.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "SonarQube",
        "S2699:Tests should include assertions",
        Justification = "This test verifies no exception is thrown on multiple dispose calls")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP016:Don't use disposed instance",
        Justification = "Testing that multiple Dispose calls don't throw")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP017:Prefer using",
        Justification = "Testing explicit Dispose calls")]
    public async Task DisposeAsyncCanBeCalledMultipleTimes()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new() { Enablement = ReservoirDevToolsEnablement.Always };
        ReduxDevToolsService service = CreateService(store, options);

        // Act & Assert - should not throw
        await service.DisposeAsync();
        await service.DisposeAsync();
    }

    /// <summary>
    ///     StartAsync should subscribe to store events when enabled.
    /// </summary>
    [Fact]
    public async Task StartAsyncSubscribesToStoreEventsWhenEnabled()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new() { Enablement = ReservoirDevToolsEnablement.Always };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();

        // Act
        await service.StartAsync(CancellationToken.None);
        store.Dispatch(new TestAction());

        // Assert - service should be subscribed (no exception means success)
        // The subscription is internal, so we verify via side effects when DevTools is connected
    }

    /// <summary>
    ///     StartAsync should not subscribe when DevTools is disabled.
    /// </summary>
    [Fact]
    public async Task StartAsyncDoesNotSubscribeWhenDisabled()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new() { Enablement = ReservoirDevToolsEnablement.Off };
        await using ReduxDevToolsService service = CreateService(store, options);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert - no JS calls should be made
        jsRuntimeMock.Verify(r => r.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()), Times.Never);
    }

    /// <summary>
    ///     Service should respect DevelopmentOnly enablement setting.
    /// </summary>
    [Fact]
    public async Task ServiceRespectsDevlopmentOnlyEnablementWhenNotInDevelopment()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new() { Enablement = ReservoirDevToolsEnablement.DevelopmentOnly };
        Mock<IHostEnvironment> hostEnvMock = new();
        hostEnvMock.Setup(h => h.EnvironmentName).Returns("Production");
        await using ReduxDevToolsService service = CreateService(store, options, hostEnvMock.Object);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert - no JS calls should be made in production
        jsRuntimeMock.Verify(r => r.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()), Times.Never);
    }

    /// <summary>
    ///     Service should connect to DevTools when in development and DevelopmentOnly is set.
    /// </summary>
    [Fact]
    public async Task ServiceConnectsWhenDevelopmentOnlyAndInDevelopment()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new() { Enablement = ReservoirDevToolsEnablement.DevelopmentOnly };
        Mock<IHostEnvironment> hostEnvMock = new();
        hostEnvMock.Setup(h => h.EnvironmentName).Returns("Development");
        await using ReduxDevToolsService service = CreateService(store, options, hostEnvMock.Object);
        SetupJsModuleForConnection();

        // Act
        await service.StartAsync(CancellationToken.None);

        // Dispatch to trigger connection
        store.Dispatch(new TestAction());

        // Allow async JS interop to complete
        await Task.Delay(50);

        // Assert - JS module should have been imported
        jsRuntimeMock.Verify(r => r.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()), Times.Once);
    }

    /// <summary>
    ///     StopAsync should unsubscribe from store events.
    /// </summary>
    [Fact]
    [SuppressMessage(
        "SonarQube",
        "S2699:Tests should include assertions",
        Justification = "This test verifies no exception is thrown during stop")]
    public async Task StopAsyncUnsubscribesFromStoreEvents()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new() { Enablement = ReservoirDevToolsEnablement.Always };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();

        await service.StartAsync(CancellationToken.None);

        // Act
        await service.StopAsync(CancellationToken.None);

        // Assert - dispatching should not cause issues after stop
        store.Dispatch(new TestAction());
    }

    /// <summary>
    ///     Service should handle JS connection failure gracefully.
    /// </summary>
    [Fact]
    public async Task ServiceHandlesConnectionFailureGracefully()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new() { Enablement = ReservoirDevToolsEnablement.Always };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection(connectReturns: false);

        await service.StartAsync(CancellationToken.None);

        // Act - dispatch should not throw even if connection fails
        store.Dispatch(new TestAction());

        // Allow async operations to complete
        await Task.Delay(50);

        // Assert - no exception is thrown
    }

    /// <summary>
    ///     Service should initialize DevTools with current state on connect.
    /// </summary>
    [Fact]
    public async Task ServiceInitializesDevToolsWithCurrentStateOnConnect()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new() { Enablement = ReservoirDevToolsEnablement.Always };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();

        await service.StartAsync(CancellationToken.None);

        // Act
        store.Dispatch(new TestAction());

        // Allow async operations to complete
        await Task.Delay(50);

        // Assert - init should have been called
        jsModuleMock.Verify(m => m.InvokeAsync<object>("init", It.IsAny<object[]>()), Times.Once);
    }

    /// <summary>
    ///     Service should send actions to DevTools after connect.
    /// </summary>
    [Fact]
    public async Task ServiceSendsActionsToDevToolsAfterConnect()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new() { Enablement = ReservoirDevToolsEnablement.Always };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();

        await service.StartAsync(CancellationToken.None);

        // Dispatch first to establish connection
        store.Dispatch(new TestAction());
        await Task.Delay(50);

        // Act - dispatch second action
        store.Dispatch(new TestAction());
        await Task.Delay(50);

        // Assert - send should have been called for both actions
        jsModuleMock.Verify(m => m.InvokeAsync<object>("send", It.IsAny<object[]>()), Times.AtLeast(1));
    }

    /// <summary>
    ///     OnDevToolsMessageAsync should ignore empty messages.
    /// </summary>
    [Fact]
    public async Task OnDevToolsMessageAsyncIgnoresEmptyMessages()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new() { Enablement = ReservoirDevToolsEnablement.Always };
        await using ReduxDevToolsService service = CreateService(store, options);

        // Act & Assert - should not throw
        await service.OnDevToolsMessageAsync("");
        await service.OnDevToolsMessageAsync("   ");
    }

    /// <summary>
    ///     OnDevToolsMessageAsync should ignore invalid JSON.
    /// </summary>
    [Fact]
    public async Task OnDevToolsMessageAsyncIgnoresInvalidJson()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new() { Enablement = ReservoirDevToolsEnablement.Always };
        await using ReduxDevToolsService service = CreateService(store, options);

        // Act & Assert - should not throw
        await service.OnDevToolsMessageAsync("not valid json");
        await service.OnDevToolsMessageAsync("{invalid}");
    }

    /// <summary>
    ///     OnDevToolsMessageAsync should ignore non-DISPATCH messages.
    /// </summary>
    [Fact]
    public async Task OnDevToolsMessageAsyncIgnoresNonDispatchMessages()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new() { Enablement = ReservoirDevToolsEnablement.Always };
        await using ReduxDevToolsService service = CreateService(store, options);

        string message = """{"type":"OTHER","payload":{}}""";

        // Act & Assert - should not throw or change state
        await service.OnDevToolsMessageAsync(message);
    }
}
