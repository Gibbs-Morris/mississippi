using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Actions;
using Mississippi.Reservoir.Abstractions.State;

using Moq;


namespace Mississippi.Reservoir.Blazor.L0Tests;

/// <summary>
///     Tests for ReduxDevToolsService strict time-travel rehydration mode.
/// </summary>
public sealed class ReduxDevToolsServiceStrictModeTests : IAsyncDisposable
{
    private readonly ReservoirDevToolsInterop interop;

    private readonly Mock<IJSObjectReference> jsModuleMock;

    private readonly Mock<IJSRuntime> jsRuntimeMock;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReduxDevToolsServiceStrictModeTests" /> class.
    /// </summary>
    public ReduxDevToolsServiceStrictModeTests()
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
        return new(registrations, Array.Empty<IMiddleware>(), TimeProvider.System);
    }

    private ReduxDevToolsService CreateService(
        IStore store,
        ReservoirDevToolsOptions options
    )
    {
        Lazy<IStore> storeFactory = new(() => store);
        return new(storeFactory, interop, Options.Create(options));
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

    private sealed record SecondFeatureState : IFeatureState
    {
        public static string FeatureKey => "second";

        public string Name { get; init; } = string.Empty;
    }

    private sealed class SecondFeatureStateRegistration : IFeatureStateRegistration
    {
        public string FeatureKey => SecondFeatureState.FeatureKey;

        public object InitialState =>
            new SecondFeatureState
            {
                Name = "initial",
            };

        public object? RootActionEffect => null;

        public object? RootReducer => null;
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
    ///     Non-strict mode should apply partial state updates.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NonStrictModeAppliesPartialStateUpdates()
    {
        // Arrange
        IFeatureStateRegistration[] registrations =
        [
            new TestFeatureStateRegistration(),
            new SecondFeatureStateRegistration(),
        ];
        using Store store = CreateStore(registrations);
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
            IsStrictStateRehydrationEnabled = false, // Non-strict mode
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        await service.StartAsync(CancellationToken.None);
        store.Dispatch(new IncrementAction());

        // Act - JUMP_TO_STATE with only 'test' feature
        string jumpMessage = """
                             {
                                 "type": "DISPATCH",
                                 "payload": { "type": "JUMP_TO_STATE" },
                                 "state": "{\"test\":{\"Value\":99}}"
                             }
                             """;
        await service.OnDevToolsMessageAsync(jumpMessage);

        // Assert - test should be updated, second unchanged
        Assert.Equal(99, store.GetState<TestFeatureState>().Value);
        Assert.Equal("initial", store.GetState<SecondFeatureState>().Name);
    }

    /// <summary>
    ///     Non-strict mode should skip features that fail deserialization.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NonStrictModeSkipsFailedDeserialization()
    {
        // Arrange
        IFeatureStateRegistration[] registrations =
        [
            new TestFeatureStateRegistration(),
            new SecondFeatureStateRegistration(),
        ];
        using Store store = CreateStore(registrations);
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
            IsStrictStateRehydrationEnabled = false,
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        await service.StartAsync(CancellationToken.None);
        store.Dispatch(new IncrementAction());

        // Act - test has invalid type, second is valid
        string jumpMessage = """
                             {
                                 "type": "DISPATCH",
                                 "payload": { "type": "JUMP_TO_STATE" },
                                 "state": "{\"test\":{\"Value\":\"not-a-number\"},\"second\":{\"Name\":\"updated\"}}"
                             }
                             """;
        await service.OnDevToolsMessageAsync(jumpMessage);

        // Assert - test unchanged (failed), second updated
        Assert.Equal(1, store.GetState<TestFeatureState>().Value);
        Assert.Equal("updated", store.GetState<SecondFeatureState>().Name);
    }

    /// <summary>
    ///     Strict mode should accept valid state with all features present.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StrictModeAcceptsValidStateWithAllFeatures()
    {
        // Arrange
        IFeatureStateRegistration[] registrations =
        [
            new TestFeatureStateRegistration(),
            new SecondFeatureStateRegistration(),
        ];
        using Store store = CreateStore(registrations);
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
            IsStrictStateRehydrationEnabled = true,
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        await service.StartAsync(CancellationToken.None);
        store.Dispatch(new IncrementAction());

        // Act - JUMP_TO_STATE with all features
        string jumpMessage = """
                             {
                                 "type": "DISPATCH",
                                 "payload": { "type": "JUMP_TO_STATE" },
                                 "state": "{\"test\":{\"Value\":42},\"second\":{\"Name\":\"restored\"}}"
                             }
                             """;
        await service.OnDevToolsMessageAsync(jumpMessage);

        // Assert - should be applied
        Assert.Equal(42, store.GetState<TestFeatureState>().Value);
        Assert.Equal("restored", store.GetState<SecondFeatureState>().Name);
    }

    /// <summary>
    ///     Strict mode defaults to disabled.
    /// </summary>
    [Fact]
    public void StrictModeDefaultsToDisabled()
    {
        // Arrange & Act
        ReservoirDevToolsOptions options = new();

        // Assert
        Assert.False(options.IsStrictStateRehydrationEnabled);
    }

    /// <summary>
    ///     Strict mode should reject array state.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StrictModeRejectsArrayState()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
            IsStrictStateRehydrationEnabled = true,
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        await service.StartAsync(CancellationToken.None);
        store.Dispatch(new IncrementAction());

        // Act - state is an array instead of object
        string jumpMessage = """
                             {
                                 "type": "DISPATCH",
                                 "payload": { "type": "JUMP_TO_STATE" },
                                 "state": "[]"
                             }
                             """;
        await service.OnDevToolsMessageAsync(jumpMessage);

        // Assert - should reject
        Assert.Equal(1, store.GetState<TestFeatureState>().Value);
    }

    /// <summary>
    ///     Strict mode should reject empty state object.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StrictModeRejectsEmptyStateObject()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
            IsStrictStateRehydrationEnabled = true,
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        await service.StartAsync(CancellationToken.None);
        store.Dispatch(new IncrementAction());
        Assert.Equal(1, store.GetState<TestFeatureState>().Value);

        // Act - empty state object
        string jumpMessage = """
                             {
                                 "type": "DISPATCH",
                                 "payload": { "type": "JUMP_TO_STATE" },
                                 "state": "{}"
                             }
                             """;
        await service.OnDevToolsMessageAsync(jumpMessage);

        // Assert - strict mode should reject
        Assert.Equal(1, store.GetState<TestFeatureState>().Value);
    }

    /// <summary>
    ///     Strict mode should reject state when deserialization fails.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StrictModeRejectsStateWhenDeserializationFails()
    {
        // Arrange
        using Store store = CreateStore();
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
            IsStrictStateRehydrationEnabled = true,
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        await service.StartAsync(CancellationToken.None);
        store.Dispatch(new IncrementAction());
        Assert.Equal(1, store.GetState<TestFeatureState>().Value);

        // Act - JUMP_TO_STATE with incompatible type (string instead of int)
        string jumpMessage = """
                             {
                                 "type": "DISPATCH",
                                 "payload": { "type": "JUMP_TO_STATE" },
                                 "state": "{\"test\":{\"Value\":\"not-a-number\"}}"
                             }
                             """;
        await service.OnDevToolsMessageAsync(jumpMessage);

        // Assert - strict mode should reject, state unchanged
        Assert.Equal(1, store.GetState<TestFeatureState>().Value);
    }

    /// <summary>
    ///     Strict mode should reject state when feature is missing from payload.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StrictModeRejectsStateWhenFeatureIsMissing()
    {
        // Arrange
        IFeatureStateRegistration[] registrations =
        [
            new TestFeatureStateRegistration(),
            new SecondFeatureStateRegistration(),
        ];
        using Store store = CreateStore(registrations);
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
            IsStrictStateRehydrationEnabled = true,
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        await service.StartAsync(CancellationToken.None);
        store.Dispatch(new IncrementAction());
        Assert.Equal(1, store.GetState<TestFeatureState>().Value);

        // Act - JUMP_TO_STATE with only 'test' feature, missing 'second'
        string jumpMessage = """
                             {
                                 "type": "DISPATCH",
                                 "payload": { "type": "JUMP_TO_STATE" },
                                 "state": "{\"test\":{\"Value\":5}}"
                             }
                             """;
        await service.OnDevToolsMessageAsync(jumpMessage);

        // Assert - strict mode should reject, state unchanged
        Assert.Equal(1, store.GetState<TestFeatureState>().Value);
        Assert.Equal("initial", store.GetState<SecondFeatureState>().Name);
    }
}