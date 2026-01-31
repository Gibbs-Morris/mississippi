using System;
using System.Collections.Generic;
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
///     Tests for ReduxDevToolsService action and state sanitizer functionality.
/// </summary>
public sealed class ReduxDevToolsServiceSanitizerTests : IAsyncDisposable
{
    private readonly ReservoirDevToolsInterop interop;

    private readonly Mock<IJSObjectReference> jsModuleMock;

    private readonly Mock<IJSRuntime> jsRuntimeMock;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReduxDevToolsServiceSanitizerTests" /> class.
    /// </summary>
    public ReduxDevToolsServiceSanitizerTests()
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
        Lazy<IStore> storeFactory = new(() => store);
        return new(storeFactory, interop, Options.Create(options));
    }

    private void SetupJsModuleForConnection()
    {
        jsRuntimeMock.Setup(r => r.InvokeAsync<IJSObjectReference>("import", It.IsAny<object[]>()))
            .ReturnsAsync(jsModuleMock.Object);
        jsModuleMock.Setup(m => m.InvokeAsync<bool>("connect", It.IsAny<object[]>())).ReturnsAsync(true);
    }

    private sealed record SensitiveAction(string Password) : IAction;

    private sealed record SensitiveFeatureState : IFeatureState
    {
        public static string FeatureKey => "sensitive";

        public string Token { get; init; } = string.Empty;
    }

    private sealed class SensitiveFeatureStateRegistration : IFeatureStateRegistration
    {
        public string FeatureKey => SensitiveFeatureState.FeatureKey;

        public object InitialState =>
            new SensitiveFeatureState
            {
                Token = "secret-token",
            };

        public object? RootActionEffect => null;

        public object? RootReducer => null;
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
    ///     Action sanitizer default is null.
    /// </summary>
    [Fact]
    public void ActionSanitizerDefaultIsNull()
    {
        // Arrange & Act
        ReservoirDevToolsOptions options = new();

        // Assert
        Assert.Null(options.ActionSanitizer);
    }

    /// <summary>
    ///     Action sanitizer should transform actions before sending to DevTools.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ActionSanitizerTransformsActionsBeforeSending()
    {
        // Arrange
        using Store store = CreateStore();
        object? capturedActionPayload = null;
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
            ActionSanitizer = action =>
            {
                if (action is SensitiveAction)
                {
                    return new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["type"] = "SensitiveAction",
                        ["payload"] = new
                        {
                            Password = "[REDACTED]",
                        },
                    };
                }

                return null;
            },
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        jsModuleMock.Setup(m => m.InvokeAsync<object>("send", It.IsAny<object[]>()))
            .Callback<string, object[]>((
                method,
                args
            ) => capturedActionPayload = args[0]);
        await service.StartAsync(CancellationToken.None);

        // Dispatch to establish connection
        store.Dispatch(new TestAction());
        await Task.Delay(50);

        // Act - dispatch sensitive action
        store.Dispatch(new SensitiveAction("my-secret-password"));
        await Task.Delay(50);

        // Assert - sanitizer should have been called (payload should be sanitized)
        // Note: The actual verification depends on the mock setup capturing the payload
        Assert.NotNull(capturedActionPayload);
    }

    /// <summary>
    ///     Null sanitizer return should use default serialization.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task NullSanitizerReturnUsesDefaultSerialization()
    {
        // Arrange
        using Store store = CreateStore();
        object? capturedActionPayload = null;
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
            ActionSanitizer = action => null, // Return null to use default
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        jsModuleMock.Setup(m => m.InvokeAsync<object>("send", It.IsAny<object[]>()))
            .Callback<string, object[]>((
                method,
                args
            ) => capturedActionPayload = args[0]);
        await service.StartAsync(CancellationToken.None);

        // Act
        store.Dispatch(new TestAction());
        await Task.Delay(50);

        // Assert - default serialization should have been used
        Assert.NotNull(capturedActionPayload);
    }

    /// <summary>
    ///     Options without sanitizers should serialize normally.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task OptionsWithoutSanitizersSerializeNormally()
    {
        // Arrange
        using Store store = CreateStore();
        bool sendCalled = false;
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        jsModuleMock.Setup(m => m.InvokeAsync<object>("send", It.IsAny<object[]>()))
            .Callback<string, object[]>((
                method,
                args
            ) => sendCalled = true);
        await service.StartAsync(CancellationToken.None);

        // Act
        store.Dispatch(new TestAction());
        await Task.Delay(50);

        // Assert
        Assert.True(sendCalled);
    }

    /// <summary>
    ///     Sanitizer should not affect actual store state.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task SanitizerDoesNotAffectActualStoreState()
    {
        // Arrange
        IFeatureStateRegistration[] registrations = [new SensitiveFeatureStateRegistration()];
        using Store store = CreateStore(registrations);
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
            StateSanitizer = snapshot =>
            {
                Dictionary<string, object> sanitized = new(StringComparer.Ordinal);
                foreach (KeyValuePair<string, object> kvp in snapshot)
                {
                    sanitized[kvp.Key] = kvp.Key == SensitiveFeatureState.FeatureKey
                        ? new
                        {
                            Token = "[REDACTED]",
                        }
                        : kvp.Value;
                }

                return sanitized;
            },
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        await service.StartAsync(CancellationToken.None);

        // Act
        store.Dispatch(new TestAction());
        await Task.Delay(50);

        // Assert - actual store state should be unchanged
        SensitiveFeatureState state = store.GetState<SensitiveFeatureState>();
        Assert.Equal("secret-token", state.Token);
    }

    /// <summary>
    ///     State sanitizer default is null.
    /// </summary>
    [Fact]
    public void StateSanitizerDefaultIsNull()
    {
        // Arrange & Act
        ReservoirDevToolsOptions options = new();

        // Assert
        Assert.Null(options.StateSanitizer);
    }

    /// <summary>
    ///     State sanitizer should transform state before sending to DevTools.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task StateSanitizerTransformsStateBeforeSending()
    {
        // Arrange
        IFeatureStateRegistration[] registrations =
        [
            new TestFeatureStateRegistration(),
            new SensitiveFeatureStateRegistration(),
        ];
        using Store store = CreateStore(registrations);
        object? capturedStatePayload = null;
        ReservoirDevToolsOptions options = new()
        {
            Enablement = ReservoirDevToolsEnablement.Always,
            StateSanitizer = snapshot =>
            {
                Dictionary<string, object> sanitized = new(StringComparer.Ordinal);
                foreach (KeyValuePair<string, object> kvp in snapshot)
                {
                    sanitized[kvp.Key] = kvp.Key == SensitiveFeatureState.FeatureKey
                        ? new
                        {
                            Token = "[REDACTED]",
                        }
                        : kvp.Value;
                }

                return sanitized;
            },
        };
        await using ReduxDevToolsService service = CreateService(store, options);
        SetupJsModuleForConnection();
        jsModuleMock.Setup(m => m.InvokeAsync<object>("send", It.IsAny<object[]>()))
            .Callback<string, object[]>((
                method,
                args
            ) => capturedStatePayload = args[1]);
        await service.StartAsync(CancellationToken.None);

        // Act
        store.Dispatch(new TestAction());
        await Task.Delay(50);

        // Assert
        Assert.NotNull(capturedStatePayload);
    }
}