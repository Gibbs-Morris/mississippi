using System;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.State;

using Moq;


namespace Mississippi.Reservoir.Blazor.L0Tests;

/// <summary>
///     Tests for ReservoirDevToolsStore strict time-travel mode.
/// </summary>
public sealed class ReservoirDevToolsStoreStrictModeTests
    : IAsyncLifetime,
      IDisposable
{
    private readonly ReservoirDevToolsInterop interop;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReservoirDevToolsStoreStrictModeTests" /> class.
    /// </summary>
    public ReservoirDevToolsStoreStrictModeTests()
    {
        Mock<IJSRuntime> jsRuntimeMock = new();
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

    private static string CreateJumpMessage(
        string stateJson
    )
    {
        // The state field in the Redux DevTools message is a JSON string containing the state.
        // We need to escape the stateJson for embedding in the outer JSON.
        string escapedState = JsonSerializer.Serialize(stateJson);
        return $$"""
                 {
                     "type": "DISPATCH",
                     "payload": {"type": "JUMP_TO_STATE"},
                     "state": {{escapedState}}
                 }
                 """;
    }

    private ReservoirDevToolsStore CreateStore(
        IFeatureStateRegistration[] registrations,
        ReservoirDevToolsOptions options
    )
    {
        Mock<IHostEnvironment> environmentMock = new();
        environmentMock.Setup(e => e.EnvironmentName).Returns("Development");
        return new(registrations, Array.Empty<IMiddleware>(), interop, Options.Create(options), environmentMock.Object);
    }

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
    ///     OnDevToolsMessageAsync without strict mode should apply partial state.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task NonStrictModeAppliesPartialState()
    {
        // Arrange
        TestFeatureState initialState = new()
        {
            Value = "Initial",
        };
        TestFeatureStateRegistration registration = new(initialState);
        ReservoirDevToolsOptions devToolsOptions = new()
        {
            Enablement = ReservoirDevToolsEnablement.Off,
            IsStrictStateRehydrationEnabled = false,
        };
        using ReservoirDevToolsStore store = CreateStore([registration], devToolsOptions);

        // State JSON has valid feature data.
        string validPayload = """{"test-feature":{"Value":"NewValue"}}""";
        string messageJson = CreateJumpMessage(validPayload);

        // Act
        await store.OnDevToolsMessageAsync(messageJson);

        // Assert - state should be updated.
        Assert.Equal("NewValue", store.GetState<TestFeatureState>().Value);
    }

    /// <summary>
    ///     OnDevToolsMessageAsync without strict mode should skip missing features.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task NonStrictModeSkipsMissingFeatures()
    {
        // Arrange
        TestFeatureState initialState = new()
        {
            Value = "Initial",
        };
        TestFeatureStateRegistration registration = new(initialState);
        ReservoirDevToolsOptions devToolsOptions = new()
        {
            Enablement = ReservoirDevToolsEnablement.Off,
            IsStrictStateRehydrationEnabled = false,
        };
        using ReservoirDevToolsStore store = CreateStore([registration], devToolsOptions);
        string originalValue = store.GetState<TestFeatureState>().Value;

        // State JSON is missing the "test-feature" key.
        string payloadWithMissingFeature = """{"OtherFeature":{"Value":"NewValue"}}""";
        string messageJson = CreateJumpMessage(payloadWithMissingFeature);

        // Act
        await store.OnDevToolsMessageAsync(messageJson);

        // Assert - state should remain unchanged (feature was not in payload).
        Assert.Equal(originalValue, store.GetState<TestFeatureState>().Value);
    }

    /// <summary>
    ///     OnDevToolsMessageAsync with strict mode should accept state when all features match.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task StrictModeAcceptsStateWhenAllFeaturesMatch()
    {
        // Arrange
        TestFeatureState initialState = new()
        {
            Value = "Initial",
        };
        TestFeatureStateRegistration registration = new(initialState);
        ReservoirDevToolsOptions devToolsOptions = new()
        {
            Enablement = ReservoirDevToolsEnablement.Off,
            IsStrictStateRehydrationEnabled = true,
        };
        using ReservoirDevToolsStore store = CreateStore([registration], devToolsOptions);

        // State JSON has all required features with valid data.
        string validPayload = """{"test-feature":{"Value":"NewValue"}}""";
        string messageJson = CreateJumpMessage(validPayload);

        // Act
        await store.OnDevToolsMessageAsync(messageJson);

        // Assert - state should be updated.
        Assert.Equal("NewValue", store.GetState<TestFeatureState>().Value);
    }

    /// <summary>
    ///     OnDevToolsMessageAsync with strict mode should reject state if deserialization fails.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task StrictModeRejectsStateWhenDeserializationFails()
    {
        // Arrange
        TestFeatureState initialState = new()
        {
            Value = "Initial",
        };
        TestFeatureStateRegistration registration = new(initialState);
        ReservoirDevToolsOptions devToolsOptions = new()
        {
            Enablement = ReservoirDevToolsEnablement.Off,
            IsStrictStateRehydrationEnabled = true,
        };
        using ReservoirDevToolsStore store = CreateStore([registration], devToolsOptions);
        string originalValue = store.GetState<TestFeatureState>().Value;

        // State JSON has invalid structure for the feature (not an object).
        string payloadWithBadStructure = """{"test-feature":"not-an-object"}""";
        string messageJson = CreateJumpMessage(payloadWithBadStructure);

        // Act
        await store.OnDevToolsMessageAsync(messageJson);

        // Assert - state should remain unchanged.
        Assert.Equal(originalValue, store.GetState<TestFeatureState>().Value);
    }

    /// <summary>
    ///     OnDevToolsMessageAsync with strict mode should reject state if a feature is missing.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task StrictModeRejectsStateWhenFeatureIsMissing()
    {
        // Arrange
        TestFeatureState initialState = new()
        {
            Value = "Initial",
        };
        TestFeatureStateRegistration registration = new(initialState);
        ReservoirDevToolsOptions devToolsOptions = new()
        {
            Enablement = ReservoirDevToolsEnablement.Off,
            IsStrictStateRehydrationEnabled = true,
        };
        using ReservoirDevToolsStore store = CreateStore([registration], devToolsOptions);
        string originalValue = store.GetState<TestFeatureState>().Value;

        // State JSON is missing the "test-feature" key entirely.
        string payloadWithMissingFeature = """{"OtherFeature":{"Value":"NewValue"}}""";
        string messageJson = CreateJumpMessage(payloadWithMissingFeature);

        // Act
        await store.OnDevToolsMessageAsync(messageJson);

        // Assert - state should remain unchanged.
        Assert.Equal(originalValue, store.GetState<TestFeatureState>().Value);
    }
}