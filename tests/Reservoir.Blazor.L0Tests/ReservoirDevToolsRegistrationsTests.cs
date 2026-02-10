using System;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

using Mississippi.Reservoir.Abstractions;
using Mississippi.Reservoir.Abstractions.Builders;

using Moq;


namespace Mississippi.Reservoir.Blazor.L0Tests;

/// <summary>
///     Tests for Reservoir DevTools registrations.
/// </summary>
public sealed class ReservoirDevToolsRegistrationsTests
{
    /// <summary>
    ///     AddReservoirDevTools should not replace the IStore registration.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task AddReservoirDevToolsDoesNotReplaceStoreAsync()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddLogging();
        TestMississippiClientBuilder builder = new(services);
        builder.AddReservoir().AddReservoirDevTools(options => options.Enablement = ReservoirDevToolsEnablement.Always);
        services.AddSingleton(new Mock<IJSRuntime>().Object);
        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        // Act
        IStore store = scope.ServiceProvider.GetRequiredService<IStore>();

        // Assert - IStore should remain the original Store type, not a derived type
        Assert.Equal("Store", store.GetType().Name);
    }

    /// <summary>
    ///     AddReservoirDevTools should register ReservoirDevToolsInterop.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task AddReservoirDevToolsRegistersInteropAsync()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddLogging();
        TestMississippiClientBuilder builder = new(services);
        builder.AddReservoir().AddReservoirDevTools();
        services.AddSingleton(new Mock<IJSRuntime>().Object);
        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        // Act
        ReservoirDevToolsInterop interop = scope.ServiceProvider.GetRequiredService<ReservoirDevToolsInterop>();

        // Assert
        Assert.NotNull(interop);
    }

    /// <summary>
    ///     AddReservoirDevTools should register ReduxDevToolsService as a scoped service.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task AddReservoirDevToolsRegistersScopedServiceAsync()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddLogging();
        TestMississippiClientBuilder builder = new(services);
        builder.AddReservoir().AddReservoirDevTools(options => options.Enablement = ReservoirDevToolsEnablement.Always);
        services.AddSingleton(new Mock<IJSRuntime>().Object);
        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        // Act
        ReduxDevToolsService devToolsService = scope.ServiceProvider.GetRequiredService<ReduxDevToolsService>();

        // Assert - ReduxDevToolsService should be resolvable as a scoped service
        Assert.NotNull(devToolsService);
    }

    /// <summary>
    ///     AddReservoirDevTools should throw when services is null.
    /// </summary>
    [Fact]
    public void AddReservoirDevToolsWithNullServicesThrows()
    {
        // Arrange
        IReservoirBuilder? builder = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder!.AddReservoirDevTools());
    }

    /// <summary>
    ///     DevToolsService should receive the same store instance as scoped component resolution.
    /// </summary>
    /// <remarks>
    ///     This test verifies the fix for the captive dependency bug where the singleton
    ///     hosted service captured a different store instance than user components.
    ///     With scoped registration, DevTools and components share the same store per scope.
    /// </remarks>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task DevToolsServiceReceivesSameStoreInstanceAsScopedComponentsAsync()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddLogging();
        TestMississippiClientBuilder builder = new(services);
        builder.AddReservoir().AddReservoirDevTools(options => options.Enablement = ReservoirDevToolsEnablement.Always);
        services.AddSingleton(new Mock<IJSRuntime>().Object);
        await using ServiceProvider provider = services.BuildServiceProvider();

        // Act - resolve store and DevToolsService in the same scope
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IStore scopedStore = scope.ServiceProvider.GetRequiredService<IStore>();
        ReduxDevToolsService devToolsService = scope.ServiceProvider.GetRequiredService<ReduxDevToolsService>();

        // Also verify different scopes get different store instances
        await using AsyncServiceScope scope2 = provider.CreateAsyncScope();
        IStore scopedStore2 = scope2.ServiceProvider.GetRequiredService<IStore>();
        ReduxDevToolsService devToolsService2 = scope2.ServiceProvider.GetRequiredService<ReduxDevToolsService>();

        // Assert - Use reflection to verify DevTools received the same store instance.
        // This validates the captive dependency fix: before the fix, the singleton service
        // would have captured a different store instance than the scoped component resolution.
        PropertyInfo? storeProperty = typeof(ReduxDevToolsService).GetProperty(
            "Store",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(storeProperty);
        IStore? devToolsStore = storeProperty.GetValue(devToolsService) as IStore;
        IStore? devToolsStore2 = storeProperty.GetValue(devToolsService2) as IStore;

        // Key assertion: DevTools in scope1 uses the same store as scope1's component resolution
        Assert.Same(scopedStore, devToolsStore);

        // Key assertion: DevTools in scope2 uses the same store as scope2's component resolution
        Assert.Same(scopedStore2, devToolsStore2);

        // Different scopes should have different store instances
        Assert.NotSame(scopedStore, scopedStore2);
        Assert.NotSame(devToolsStore, devToolsStore2);
    }

    /// <summary>
    ///     Multiple scopes should each get their own DevToolsService instance.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task EachScopeGetsOwnDevToolsServiceInstanceAsync()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddLogging();
        TestMississippiClientBuilder builder = new(services);
        builder.AddReservoir().AddReservoirDevTools(options => options.Enablement = ReservoirDevToolsEnablement.Always);
        services.AddSingleton(new Mock<IJSRuntime>().Object);
        await using ServiceProvider provider = services.BuildServiceProvider();

        // Act - resolve DevToolsService from two different scopes
        await using AsyncServiceScope scope1 = provider.CreateAsyncScope();
        await using AsyncServiceScope scope2 = provider.CreateAsyncScope();
        ReduxDevToolsService devTools1 = scope1.ServiceProvider.GetRequiredService<ReduxDevToolsService>();
        ReduxDevToolsService devTools2 = scope2.ServiceProvider.GetRequiredService<ReduxDevToolsService>();

        // Assert - each scope should have its own DevToolsService
        Assert.NotSame(devTools1, devTools2);
    }
}