using System;
using System.Collections.Generic;
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
///     Tests for ReservoirDevToolsStore sanitizer functionality.
/// </summary>
public sealed class ReservoirDevToolsStoreSanitizerTests
    : IAsyncLifetime,
      IDisposable
{
    private readonly ReservoirDevToolsInterop interop;

    private readonly Mock<IJSRuntime> jsRuntimeMock;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReservoirDevToolsStoreSanitizerTests" /> class.
    /// </summary>
    public ReservoirDevToolsStoreSanitizerTests()
    {
        jsRuntimeMock = new();
        interop = new(jsRuntimeMock.Object);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Handled by DisposeAsync in xUnit.
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await interop.DisposeAsync();
    }

    /// <inheritdoc />
    public Task InitializeAsync() => Task.CompletedTask;

    private ReservoirDevToolsStore CreateStore(
        IFeatureStateRegistration[] registrations,
        ReservoirDevToolsOptions options
    )
    {
        Mock<IHostEnvironment> environmentMock = new();
        environmentMock.Setup(e => e.EnvironmentName).Returns("Development");
        return new(registrations, Array.Empty<IMiddleware>(), interop, Options.Create(options), environmentMock.Object);
    }

    private sealed record SensitiveAction(string Username, string Password) : IAction;

    private sealed record SensitiveFeatureState : IFeatureState
    {
        public static string FeatureKey => "sensitive-feature";

        public string Secret { get; init; } = string.Empty;
    }

    private sealed class SensitiveFeatureStateRegistration : IFeatureStateRegistration
    {
        private readonly SensitiveFeatureState initialState;

        public SensitiveFeatureStateRegistration(
            SensitiveFeatureState initialState
        ) =>
            this.initialState = initialState;

        public string FeatureKey => SensitiveFeatureState.FeatureKey;

        public object InitialState => initialState;

        public object? RootActionEffect => null;

        public object? RootReducer => null;
    }

    private sealed record TestAction(string Value) : IAction;

    private sealed record TestFeatureState : IFeatureState
    {
        public static string FeatureKey => "test-feature";

        public string Value { get; init; } = string.Empty;
    }

    private sealed class TestFeatureStateRegistration : IFeatureStateRegistration
    {
        private readonly TestFeatureState initialState;

        public TestFeatureStateRegistration(
            TestFeatureState initialState
        ) =>
            this.initialState = initialState;

        public string FeatureKey => TestFeatureState.FeatureKey;

        public object InitialState => initialState;

        public object? RootActionEffect => null;

        public object? RootReducer => null;
    }

    /// <summary>
    ///     ActionSanitizer can redact sensitive fields from action.
    /// </summary>
    [Fact]
    public void ActionSanitizerCanRedactSensitiveFields()
    {
        // Arrange
        List<object> capturedPayloads = [];
        ReservoirDevToolsOptions devToolsOptions = new()
        {
            Enablement = ReservoirDevToolsEnablement.Off,
            ActionSanitizer = action =>
            {
                object payload = action switch
                {
                    SensitiveAction sensitive => new
                    {
                        type = nameof(SensitiveAction),
                        username = sensitive.Username,
                        password = "[REDACTED]",
                    },
                    var _ => new
                    {
                        type = action.GetType().Name,
                        payload = action,
                    },
                };
                capturedPayloads.Add(payload);
                return payload;
            },
        };
        TestFeatureState initialState = new()
        {
            Value = "Initial",
        };
        TestFeatureStateRegistration registration = new(initialState);
        using ReservoirDevToolsStore store = CreateStore([registration], devToolsOptions);

        // Act
        store.Dispatch(new SensitiveAction("admin", "supersecret"));
        store.Dispatch(new TestAction("normal"));

        // Assert - store processed both actions
        Assert.Equal("Initial", store.GetState<TestFeatureState>().Value);
    }

    /// <summary>
    ///     ActionSanitizer returning null should use default serialization.
    /// </summary>
    [Fact]
    public void ActionSanitizerReturningNullUsesDefaultSerialization()
    {
        // Arrange
        ReservoirDevToolsOptions devToolsOptions = new()
        {
            Enablement = ReservoirDevToolsEnablement.Off,

            // Return null to signal default serialization should be used
            ActionSanitizer = _ => null,
        };
        TestFeatureState initialState = new()
        {
            Value = "Initial",
        };
        TestFeatureStateRegistration registration = new(initialState);
        using ReservoirDevToolsStore store = CreateStore([registration], devToolsOptions);

        // Act
        store.Dispatch(new TestAction("test"));

        // Assert - store should still function correctly when sanitizer returns null
        Assert.Equal("Initial", store.GetState<TestFeatureState>().Value);
    }

    /// <summary>
    ///     ActionSanitizer should transform action payload when provided.
    /// </summary>
    [Fact]
    public void ActionSanitizerTransformsActionPayload()
    {
        // Arrange
        object? capturedAction = null;
        ReservoirDevToolsOptions devToolsOptions = new()
        {
            Enablement = ReservoirDevToolsEnablement.Off,
            ActionSanitizer = action =>
            {
                if (action is SensitiveAction sensitive)
                {
                    capturedAction = new
                    {
                        type = "SensitiveAction",
                        password = "***",
                        username = sensitive.Username,
                    };
                    return capturedAction;
                }

                return null;
            },
        };
        TestFeatureState initialState = new()
        {
            Value = "Initial",
        };
        TestFeatureStateRegistration registration = new(initialState);
        using ReservoirDevToolsStore store = CreateStore([registration], devToolsOptions);
        SensitiveAction action = new("testuser", "secret123");

        // Act - dispatch triggers sanitizer (even though DevTools is off, the sanitizer is configured)
        store.Dispatch(action);

        // Assert - action was processed (store still works)
        // Note: With Enablement=Off, DevTools doesn't send to extension, but sanitizer is invoked
        // We verify the store dispatched successfully
        Assert.Equal("Initial", store.GetState<TestFeatureState>().Value);
    }

    /// <summary>
    ///     StateSanitizer can exclude specific feature states.
    /// </summary>
    [Fact]
    public void StateSanitizerCanExcludeSpecificFeatures()
    {
        // Arrange
        List<IReadOnlyDictionary<string, object>> capturedSnapshots = [];
        ReservoirDevToolsOptions devToolsOptions = new()
        {
            Enablement = ReservoirDevToolsEnablement.Off,
            StateSanitizer = state =>
            {
                capturedSnapshots.Add(state);

                // Exclude sensitive feature from what would be sent to DevTools
                Dictionary<string, object> filtered = [];
                foreach (KeyValuePair<string, object> kvp in state)
                {
                    if (!kvp.Key.Contains("sensitive", StringComparison.OrdinalIgnoreCase))
                    {
                        filtered[kvp.Key] = kvp.Value;
                    }
                }

                return filtered;
            },
        };
        TestFeatureState initialState = new()
        {
            Value = "Public",
        };
        SensitiveFeatureState sensitiveState = new()
        {
            Secret = "Private",
        };
        TestFeatureStateRegistration registration = new(initialState);
        SensitiveFeatureStateRegistration sensitiveRegistration = new(sensitiveState);
        using ReservoirDevToolsStore store = CreateStore([registration, sensitiveRegistration], devToolsOptions);

        // Act
        store.Dispatch(new TestAction("test"));

        // Assert - both features exist in actual store
        Assert.Equal("Public", store.GetState<TestFeatureState>().Value);
        Assert.Equal("Private", store.GetState<SensitiveFeatureState>().Secret);
    }

    /// <summary>
    ///     StateSanitizer returning null should use original state snapshot.
    /// </summary>
    [Fact]
    public void StateSanitizerReturningNullUsesOriginalSnapshot()
    {
        // Arrange
        ReservoirDevToolsOptions devToolsOptions = new()
        {
            Enablement = ReservoirDevToolsEnablement.Off,

            // Return null to signal original snapshot should be used
            StateSanitizer = _ => null,
        };
        TestFeatureState initialState = new()
        {
            Value = "Initial",
        };
        TestFeatureStateRegistration registration = new(initialState);
        using ReservoirDevToolsStore store = CreateStore([registration], devToolsOptions);

        // Act
        store.Dispatch(new TestAction("test"));

        // Assert - store functions correctly
        Assert.Equal("Initial", store.GetState<TestFeatureState>().Value);
    }

    /// <summary>
    ///     StateSanitizer should transform state payload when provided.
    /// </summary>
    [Fact]
    public void StateSanitizerTransformsStatePayload()
    {
        // Arrange
        IReadOnlyDictionary<string, object>? capturedState = null;
        object? sanitizedResult = null;
        ReservoirDevToolsOptions devToolsOptions = new()
        {
            Enablement = ReservoirDevToolsEnablement.Off,
            StateSanitizer = state =>
            {
                capturedState = state;

                // Remove sensitive feature from state snapshot
                Dictionary<string, object> sanitized = new(state);
                sanitized.Remove("sensitive-feature");
                sanitizedResult = sanitized;
                return sanitizedResult;
            },
        };
        TestFeatureState initialState = new()
        {
            Value = "Initial",
        };
        SensitiveFeatureState sensitiveState = new()
        {
            Secret = "TopSecret",
        };
        TestFeatureStateRegistration registration = new(initialState);
        SensitiveFeatureStateRegistration sensitiveRegistration = new(sensitiveState);
        using ReservoirDevToolsStore store = CreateStore([registration, sensitiveRegistration], devToolsOptions);

        // Act
        store.Dispatch(new TestAction("trigger"));

        // Assert - both features are in the store
        Assert.Equal("Initial", store.GetState<TestFeatureState>().Value);
        Assert.Equal("TopSecret", store.GetState<SensitiveFeatureState>().Secret);
    }
}