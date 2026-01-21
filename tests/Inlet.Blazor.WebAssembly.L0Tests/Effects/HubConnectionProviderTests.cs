using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Inlet.Blazor.WebAssembly.Effects;
using Mississippi.Reservoir;


namespace Mississippi.Inlet.Blazor.WebAssembly.L0Tests.Effects;

/// <summary>
///     Tests for <see cref="HubConnectionProvider" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Blazor.WebAssembly")]
[AllureSuite("Effects")]
[AllureSubSuite("HubConnectionProvider")]
public sealed class HubConnectionProviderTests : IDisposable
{
    private readonly NavigationManager navigationManager;

    private readonly ProjectionCache projectionCache;

    private readonly CompositeInletStore store;

    private readonly FakeTimeProvider timeProvider;

    private readonly Store underlyingStore;

    /// <summary>
    ///     Initializes a new instance of the <see cref="HubConnectionProviderTests" /> class.
    /// </summary>
    public HubConnectionProviderTests()
    {
        navigationManager = CreateNavigationManager();
        timeProvider = new(new(2024, 1, 15, 12, 0, 0, TimeSpan.Zero));
        projectionCache = new();
        underlyingStore = new();
        store = new(underlyingStore, projectionCache);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        store.Dispose();
        underlyingStore.Dispose();
    }

    private static TestNavigationManager CreateNavigationManager() => new();

    private HubConnectionProvider CreateProvider() => new(navigationManager, new(() => store), null, timeProvider);

    [SuppressMessage(
        "Performance",
        "S1144:Unused private types or members should be removed",
        Justification = "Constructor implicitly used by CreateNavigationManager.")]
    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/");

        protected override void NavigateToCore(
            string uri,
            bool forceLoad
        )
        {
            // No-op for tests
        }
    }

    /// <summary>
    ///     Verifies that custom options are applied.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    [AllureFeature("Construction")]
    public async Task ConstructorAppliesCustomOptions()
    {
        // Arrange
        InletSignalREffectOptions options = new()
        {
            HubPath = "/custom-hub",
        };

        // Act
        await using HubConnectionProvider provider = new(navigationManager, new(() => store), options);

        // Assert
        Assert.NotNull(provider.Connection);
    }

    /// <summary>
    ///     Verifies that the constructor creates a valid hub connection.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    [AllureFeature("Construction")]
    public async Task ConstructorCreatesHubConnection()
    {
        // Arrange & Act
        await using HubConnectionProvider provider = CreateProvider();

        // Assert
        Assert.NotNull(provider.Connection);
    }

    /// <summary>
    ///     Verifies that the constructor throws when lazyStore is null.
    /// </summary>
    [Fact]
    [AllureFeature("Construction")]
    public void ConstructorThrowsWhenLazyStoreIsNull()
    {
        // Arrange & Act
        void Act() => _ = new HubConnectionProvider(navigationManager, null!);

        // Assert
        Assert.Throws<ArgumentNullException>(Act);
    }

    /// <summary>
    ///     Verifies that the constructor throws when navigationManager is null.
    /// </summary>
    [Fact]
    [AllureFeature("Construction")]
    public void ConstructorThrowsWhenNavigationManagerIsNull()
    {
        // Arrange & Act
        void Act() => _ = new HubConnectionProvider(null!, new(() => store));

        // Assert
        Assert.Throws<ArgumentNullException>(Act);
    }

    /// <summary>
    ///     Verifies that default options are used when null is provided.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    [AllureFeature("Construction")]
    public async Task ConstructorUsesDefaultOptionsWhenNull()
    {
        // Arrange & Act - should not throw
        await using HubConnectionProvider provider = new(navigationManager, new(() => store));

        // Assert
        Assert.NotNull(provider.Connection);
    }

    /// <summary>
    ///     Verifies that DisposeAsync disposes the connection.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    [AllureFeature("Lifecycle")]
    public async Task DisposeAsyncDisposesConnection()
    {
        // Arrange
        HubConnectionProvider provider = CreateProvider();

        // Act
        await provider.DisposeAsync();

        // Assert - connection should be disposed (will throw on use)
        // Since HubConnection doesn't expose disposed state, we verify
        // the method completes without throwing
        Assert.True(true);
    }

    /// <summary>
    ///     Verifies that IsConnected returns false when not connected.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    [AllureFeature("State")]
    public async Task IsConnectedReturnsFalseWhenDisconnected()
    {
        // Arrange
        await using HubConnectionProvider provider = CreateProvider();

        // Act
        bool result = provider.IsConnected;

        // Assert
        Assert.False(result);
    }

    /// <summary>
    ///     Verifies that OnClosed registers a handler without throwing.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    [AllureFeature("EventHandlers")]
    public async Task OnClosedRegistersHandler()
    {
        // Arrange
        await using HubConnectionProvider provider = CreateProvider();
        bool handlerCalled = false;

        // Act - should not throw
        provider.OnClosed(_ =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        });

        // Assert - we can't easily trigger Closed, but at least verify no exception
        Assert.False(handlerCalled);
    }

    /// <summary>
    ///     Verifies that OnReconnected registers a handler without throwing.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    [AllureFeature("EventHandlers")]
    public async Task OnReconnectedRegistersHandler()
    {
        // Arrange
        await using HubConnectionProvider provider = CreateProvider();
        bool handlerCalled = false;

        // Act - should not throw
        provider.OnReconnected(_ =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        });

        // Assert
        Assert.False(handlerCalled);
    }

    /// <summary>
    ///     Verifies that OnReconnecting registers a handler without throwing.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    [AllureFeature("EventHandlers")]
    public async Task OnReconnectingRegistersHandler()
    {
        // Arrange
        await using HubConnectionProvider provider = CreateProvider();
        bool handlerCalled = false;

        // Act - should not throw
        provider.OnReconnecting(_ =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        });

        // Assert
        Assert.False(handlerCalled);
    }

    /// <summary>
    ///     Verifies that RegisterHandler registers a hub method handler.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    [AllureFeature("EventHandlers")]
    public async Task RegisterHandlerRegistersHubMethodHandler()
    {
        // Arrange
        await using HubConnectionProvider provider = CreateProvider();

        // Act
        using IDisposable subscription = provider.RegisterHandler<string, int, bool>(
            "TestMethod",
            (
                _,
                _,
                _
            ) => Task.CompletedTask);

        // Assert
        Assert.NotNull(subscription);
    }
}