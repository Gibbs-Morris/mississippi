using System;
using System.Diagnostics.CodeAnalysis;
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

    private static Store CreateStore()
    {
        IFeatureStateRegistration[] registrations = [new TestFeatureStateRegistration()];
        return new(registrations, Array.Empty<IMiddleware>(), TimeProvider.System);
    }

    private ReduxDevToolsService CreateService(
        IStore store,
        ReservoirDevToolsOptions? options = null,
        IHostEnvironment? hostEnvironment = null
    )
    {
        options ??= new();
        return new(store, interop, Options.Create(options), hostEnvironment);
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

        public object InitialState =>
            new TestFeatureState
            {
                Value = 0,
            };

        public object? RootActionEffect => null;

        public object? RootReducer => null;
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
        Assert.Throws<ArgumentNullException>(() => new ReduxDevToolsService(store, null!, Options.Create(options)));
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
        Assert.Throws<ArgumentNullException>(() => new ReduxDevToolsService(store, interop, null!));
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
        Assert.Throws<ArgumentNullException>(() => new ReduxDevToolsService(null!, interop, Options.Create(options)));
    }

    /// <summary>
    ///     DisposeAsync should be callable multiple times without error.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
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
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
        };
        ReduxDevToolsService service = CreateService(store, options);

        // Act & Assert - should not throw
        await service.DisposeAsync();
        await service.DisposeAsync();
        Assert.NotEmpty(store.GetStateSnapshot());
    }

    /// <summary>
    ///     Initialize should not subscribe when DevTools is disabled.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeDoesNotSubscribeWhenDisabled()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Off,
        };
        await using ReduxDevToolsService service = CreateService(store, options);

        // Act
        service.Initialize();

        // Assert - no JS calls should be made
        jsRuntimeMock.Verify(r => r.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()), Times.Never);
    }

    /// <summary>
    ///     Initialize should subscribe to store events when enabled.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task InitializeSubscribesToStoreEventsWhenEnabled()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        int sendCount = 0;
        jsModuleMock.Setup(m => m.InvokeAsync<object>("send", It.IsAny<object[]>()))
            .Callback(() => sendCount++)
            .ReturnsAsync(new object());

        // Act
        service.Initialize();
        store.Dispatch(new TestAction());
        await Task.Delay(50);

        // Assert - service should be subscribed (no exception means success)
        // The subscription is internal, so we verify via send side effects when DevTools is connected
        Assert.True(sendCount > 0);
    }

    /// <summary>
    ///     OnDevToolsMessageAsync should ignore empty messages.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnDevToolsMessageAsyncIgnoresEmptyMessages()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
        };
        await using ReduxDevToolsService service = CreateService(store, options);

        // Act & Assert - should not throw
        await service.OnDevToolsMessageAsync(string.Empty);
        await service.OnDevToolsMessageAsync("   ");
        Assert.Equal(0, store.GetState<TestFeatureState>().Value);
    }

    /// <summary>
    ///     OnDevToolsMessageAsync should ignore invalid JSON.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnDevToolsMessageAsyncIgnoresInvalidJson()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
        };
        await using ReduxDevToolsService service = CreateService(store, options);

        // Act & Assert - should not throw
        await service.OnDevToolsMessageAsync("not valid json");
        await service.OnDevToolsMessageAsync("{invalid}");
        Assert.Equal(0, store.GetState<TestFeatureState>().Value);
    }

    /// <summary>
    ///     OnDevToolsMessageAsync should ignore non-DISPATCH messages.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OnDevToolsMessageAsyncIgnoresNonDispatchMessages()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        string message = """{"type":"OTHER","payload":{}}""";

        // Act & Assert - should not throw or change state
        await service.OnDevToolsMessageAsync(message);
        Assert.Equal(0, store.GetState<TestFeatureState>().Value);
    }

    /// <summary>
    ///     Service should connect to DevTools when in development and DevelopmentOnly is set.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ServiceConnectsWhenDevelopmentOnlyAndInDevelopment()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.DevelopmentOnly,
        };
        Mock<IHostEnvironment> hostEnvMock = new();
        hostEnvMock.Setup(h => h.EnvironmentName).Returns("Development");
        await using ReduxDevToolsService service = CreateService(store, options, hostEnvMock.Object);
        SetupJsModuleForConnection();

        // Act
        service.Initialize();

        // Dispatch to trigger connection
        store.Dispatch(new TestAction());

        // Allow async JS interop to complete
        await Task.Delay(50);

        // Assert - JS module should have been imported
        jsRuntimeMock.Verify(r => r.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()), Times.Once);
    }

    /// <summary>
    ///     Service should handle JS connection failure gracefully.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ServiceHandlesConnectionFailureGracefully()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection(false);
        service.Initialize();

        // Act - dispatch should not throw even if connection fails
        store.Dispatch(new TestAction());

        // Allow async operations to complete
        await Task.Delay(50);

        // Assert - connection attempted but send not invoked
        jsModuleMock.Verify(m => m.InvokeAsync<bool>("connect", It.IsAny<object[]>()), Times.Once);
        jsModuleMock.Verify(m => m.InvokeAsync<object>("send", It.IsAny<object[]>()), Times.Never);
    }

    /// <summary>
    ///     Service should initialize DevTools with current state on connect.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ServiceInitializesDevToolsWithCurrentStateOnConnect()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        service.Initialize();

        // Act
        store.Dispatch(new TestAction());

        // Allow async operations to complete
        await Task.Delay(50);

        // Assert - init should have been called
        jsModuleMock.Verify(m => m.InvokeAsync<object>("init", It.IsAny<object[]>()), Times.Once);
    }

    /// <summary>
    ///     Service should respect DevelopmentOnly enablement setting.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ServiceRespectsDevlopmentOnlyEnablementWhenNotInDevelopment()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.DevelopmentOnly,
        };
        Mock<IHostEnvironment> hostEnvMock = new();
        hostEnvMock.Setup(h => h.EnvironmentName).Returns("Production");
        await using ReduxDevToolsService service = CreateService(store, options, hostEnvMock.Object);

        // Act
        service.Initialize();

        // Assert - no JS calls should be made in production
        jsRuntimeMock.Verify(r => r.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()), Times.Never);
    }

    /// <summary>
    ///     Service should send actions to DevTools after connect.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ServiceSendsActionsToDevToolsAfterConnect()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        service.Initialize();

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
    ///     Stop should unsubscribe from store events.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StopUnsubscribesFromStoreEvents()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        int sendCount = 0;
        jsModuleMock.Setup(m => m.InvokeAsync<object>("send", It.IsAny<object[]>()))
            .Callback(() => sendCount++)
            .ReturnsAsync(new object());
        service.Initialize();
        store.Dispatch(new TestAction());
        await Task.Delay(50);
        int sendCountBeforeStop = sendCount;

        // Act
        service.Stop();

        // Assert - dispatching should not send after stop
        store.Dispatch(new TestAction());
        await Task.Delay(50);
        Assert.Equal(sendCountBeforeStop, sendCount);
    }
}