using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;

using Moq;


namespace Mississippi.Reservoir.Blazor.L0Tests;

/// <summary>
///     Tests for ReduxDevToolsService message handling (RESET, ROLLBACK, COMMIT, IMPORT_STATE).
/// </summary>
public sealed class ReduxDevToolsServiceMessageHandlerTests : IAsyncDisposable
{
    private readonly ReservoirDevToolsInterop interop;

    private readonly Mock<IJSObjectReference> jsModuleMock;

    private readonly Mock<IJSRuntime> jsRuntimeMock;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReduxDevToolsServiceMessageHandlerTests" /> class.
    /// </summary>
    public ReduxDevToolsServiceMessageHandlerTests()
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

    private static Store CreateStore(
        IFeatureStateRegistration[]? registrations = null
    )
    {
        registrations ??= [new TestFeatureStateRegistration()];
        return new(registrations, Array.Empty<IMiddleware>());
    }

    private ReduxDevToolsService CreateService(
        IStore store,
        ReservoirDevToolsOptions options
    )
    {
        Mock<IServiceProvider> serviceProviderMock = new();
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IStore))).Returns(store);
        return new(serviceProviderMock.Object, interop, Options.Create(options));
    }

    private void SetupJsModuleForConnection()
    {
        jsRuntimeMock.Setup(r => r.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
            .ReturnsAsync(jsModuleMock.Object);
        jsModuleMock.Setup(m => m.InvokeAsync<bool>("connect", It.IsAny<object[]>())).ReturnsAsync(true);
    }

    private sealed record IncrementAction : IAction;

    private sealed class IncrementReducer : ActionReducerBase<IncrementAction, TestFeatureState>
    {
        public override TestFeatureState Reduce(
            TestFeatureState state,
            IncrementAction action
        ) =>
            state with
            {
                Value = state.Value + 1,
            };
    }

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

        public object RootReducer { get; } = new RootReducer<TestFeatureState>([new IncrementReducer()]);
    }

    /// <summary>
    ///     COMMIT should update the committed snapshot.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CommitUpdatesCommittedSnapshot()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        await service.StartAsync(CancellationToken.None);
        store.Dispatch(new IncrementAction());
        store.Dispatch(new IncrementAction());
        Assert.Equal(2, store.GetState<TestFeatureState>().Value);

        // Act - simulate COMMIT
        string commitMessage = """
                               {
                                   "type": "DISPATCH",
                                   "payload": { "type": "COMMIT" }
                               }
                               """;
        await service.OnDevToolsMessageAsync(commitMessage);

        // Now increment more
        store.Dispatch(new IncrementAction());
        Assert.Equal(3, store.GetState<TestFeatureState>().Value);

        // Rollback should now go to committed value=2
        string rollbackMessage = """
                                 {
                                     "type": "DISPATCH",
                                     "payload": { "type": "ROLLBACK" }
                                 }
                                 """;
        await service.OnDevToolsMessageAsync(rollbackMessage);

        // Assert
        Assert.Equal(2, store.GetState<TestFeatureState>().Value);
    }

    /// <summary>
    ///     IMPORT_STATE should apply the imported state.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ImportStateAppliesImportedState()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        await service.StartAsync(CancellationToken.None);
        store.Dispatch(new IncrementAction());
        Assert.Equal(1, store.GetState<TestFeatureState>().Value);

        // Act - simulate IMPORT_STATE
        string importMessage = """
                               {
                                   "type": "DISPATCH",
                                   "payload": {
                                       "type": "IMPORT_STATE",
                                       "nextLiftedState": {
                                           "computedStates": [
                                               { "state": { "test": { "Value": 42 } } }
                                           ]
                                       }
                                   }
                               }
                               """;
        await service.OnDevToolsMessageAsync(importMessage);

        // Assert - state should be imported
        Assert.Equal(42, store.GetState<TestFeatureState>().Value);
    }

    /// <summary>
    ///     IMPORT_STATE should use the last computed state when multiple exist.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ImportStateUsesLastComputedState()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        await service.StartAsync(CancellationToken.None);

        // Act - simulate IMPORT_STATE with multiple computed states
        string importMessage = """
                               {
                                   "type": "DISPATCH",
                                   "payload": {
                                       "type": "IMPORT_STATE",
                                       "nextLiftedState": {
                                           "computedStates": [
                                               { "state": { "test": { "Value": 10 } } },
                                               { "state": { "test": { "Value": 20 } } },
                                               { "state": { "test": { "Value": 30 } } }
                                           ]
                                       }
                                   }
                               }
                               """;
        await service.OnDevToolsMessageAsync(importMessage);

        // Assert - should use last state (30)
        Assert.Equal(30, store.GetState<TestFeatureState>().Value);
    }

    /// <summary>
    ///     JUMP_TO_ACTION should restore state from the message.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task JumpToActionRestoresStateFromMessage()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        await service.StartAsync(CancellationToken.None);
        store.Dispatch(new IncrementAction());
        store.Dispatch(new IncrementAction());
        store.Dispatch(new IncrementAction());
        Assert.Equal(3, store.GetState<TestFeatureState>().Value);

        // Act - simulate JUMP_TO_ACTION
        string jumpMessage = """
                             {
                                 "type": "DISPATCH",
                                 "payload": { "type": "JUMP_TO_ACTION" },
                                 "state": "{\"test\":{\"Value\":2}}"
                             }
                             """;
        await service.OnDevToolsMessageAsync(jumpMessage);

        // Assert
        Assert.Equal(2, store.GetState<TestFeatureState>().Value);
    }

    /// <summary>
    ///     JUMP_TO_STATE should restore state from the message.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task JumpToStateRestoresStateFromMessage()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        await service.StartAsync(CancellationToken.None);

        // Dispatch some actions to change state
        store.Dispatch(new IncrementAction());
        store.Dispatch(new IncrementAction());
        Assert.Equal(2, store.GetState<TestFeatureState>().Value);

        // Act - simulate JUMP_TO_STATE from DevTools with state value=1
        string jumpMessage = """
                             {
                                 "type": "DISPATCH",
                                 "payload": { "type": "JUMP_TO_STATE" },
                                 "state": "{\"test\":{\"Value\":1}}"
                             }
                             """;
        await service.OnDevToolsMessageAsync(jumpMessage);

        // Assert - state should be restored to value=1
        Assert.Equal(1, store.GetState<TestFeatureState>().Value);
    }

    /// <summary>
    ///     Message with missing state field should be ignored for JUMP messages.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task JumpWithMissingStateIsIgnored()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        await service.StartAsync(CancellationToken.None);
        store.Dispatch(new IncrementAction());
        Assert.Equal(1, store.GetState<TestFeatureState>().Value);

        // Act - JUMP_TO_STATE without state field
        string jumpMessage = """
                             {
                                 "type": "DISPATCH",
                                 "payload": { "type": "JUMP_TO_STATE" }
                             }
                             """;
        await service.OnDevToolsMessageAsync(jumpMessage);

        // Assert - state unchanged
        Assert.Equal(1, store.GetState<TestFeatureState>().Value);
    }

    /// <summary>
    ///     Message with malformed state JSON should be ignored.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task MalformedStateJsonIsIgnored()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        await service.StartAsync(CancellationToken.None);
        store.Dispatch(new IncrementAction());
        Assert.Equal(1, store.GetState<TestFeatureState>().Value);

        // Act - JUMP_TO_STATE with malformed state
        string jumpMessage = """
                             {
                                 "type": "DISPATCH",
                                 "payload": { "type": "JUMP_TO_STATE" },
                                 "state": "not valid json"
                             }
                             """;
        await service.OnDevToolsMessageAsync(jumpMessage);

        // Assert - state unchanged
        Assert.Equal(1, store.GetState<TestFeatureState>().Value);
    }

    /// <summary>
    ///     Message with null payload type should be ignored.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NullPayloadTypeIsIgnored()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        await service.StartAsync(CancellationToken.None);
        store.Dispatch(new IncrementAction());
        Assert.Equal(1, store.GetState<TestFeatureState>().Value);

        // Act - DISPATCH with no payload type
        string message = """
                         {
                             "type": "DISPATCH",
                             "payload": { }
                         }
                         """;
        await service.OnDevToolsMessageAsync(message);

        // Assert - state unchanged
        Assert.Equal(1, store.GetState<TestFeatureState>().Value);
    }

    /// <summary>
    ///     RESET should restore state to initial values.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ResetRestoresStateToInitialValues()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        await service.StartAsync(CancellationToken.None);
        store.Dispatch(new IncrementAction());
        store.Dispatch(new IncrementAction());
        Assert.Equal(2, store.GetState<TestFeatureState>().Value);

        // Act - simulate RESET from DevTools
        string resetMessage = """
                              {
                                  "type": "DISPATCH",
                                  "payload": { "type": "RESET" }
                              }
                              """;
        await service.OnDevToolsMessageAsync(resetMessage);

        // Assert - state should be back to initial (0)
        Assert.Equal(0, store.GetState<TestFeatureState>().Value);
    }

    /// <summary>
    ///     ROLLBACK should restore state to committed snapshot.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task RollbackRestoresStateToCommittedSnapshot()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        await service.StartAsync(CancellationToken.None);

        // Initial committed snapshot is at value=0
        store.Dispatch(new IncrementAction());
        store.Dispatch(new IncrementAction());
        Assert.Equal(2, store.GetState<TestFeatureState>().Value);

        // Act - simulate ROLLBACK
        string rollbackMessage = """
                                 {
                                     "type": "DISPATCH",
                                     "payload": { "type": "ROLLBACK" }
                                 }
                                 """;
        await service.OnDevToolsMessageAsync(rollbackMessage);

        // Assert - should be back to committed snapshot (0)
        Assert.Equal(0, store.GetState<TestFeatureState>().Value);
    }

    /// <summary>
    ///     DISPATCH with unknown payload type should be ignored.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task UnknownPayloadTypeIsIgnored()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        await service.StartAsync(CancellationToken.None);
        store.Dispatch(new IncrementAction());
        Assert.Equal(1, store.GetState<TestFeatureState>().Value);

        // Act
        string message = """
                         {
                             "type": "DISPATCH",
                             "payload": { "type": "UNKNOWN_TYPE" }
                         }
                         """;
        await service.OnDevToolsMessageAsync(message);

        // Assert - state unchanged
        Assert.Equal(1, store.GetState<TestFeatureState>().Value);
    }
}