using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;

using Mississippi.Reservoir.Abstractions;

using Moq;


namespace Mississippi.Reservoir.Blazor.L0Tests;

/// <summary>
///     Tests for Reservoir DevTools registrations.
/// </summary>
public sealed class ReservoirDevToolsRegistrationsTests
{
    /// <summary>
    ///     AddReservoirDevTools should register ReduxDevToolsService as a hosted service.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task AddReservoirDevToolsRegistersHostedServiceAsync()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddReservoir();
        services.AddReservoirDevTools(options => options.Enablement = ReservoirDevToolsEnablement.Always);
        services.AddSingleton(new Mock<IJSRuntime>().Object);
        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        // Act
        IEnumerable<IHostedService> hostedServices = scope.ServiceProvider.GetServices<IHostedService>();

        // Assert - ReduxDevToolsService should be registered as IHostedService
        Assert.Contains(hostedServices, s => s.GetType().Name == "ReduxDevToolsService");
    }

    /// <summary>
    ///     AddReservoirDevTools should not replace the IStore registration.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task AddReservoirDevToolsDoesNotReplaceStoreAsync()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddReservoir();
        services.AddReservoirDevTools(options => options.Enablement = ReservoirDevToolsEnablement.Always);
        services.AddSingleton(new Mock<IJSRuntime>().Object);
        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        // Act
        IStore store = scope.ServiceProvider.GetRequiredService<IStore>();

        // Assert - IStore should remain the original Store type, not a derived type
        Assert.Equal("Store", store.GetType().Name);
    }

    /// <summary>
    ///     AddReservoirDevTools should throw when services is null.
    /// </summary>
    [Fact]
    public void AddReservoirDevToolsWithNullServicesThrows()
    {
        // Arrange
        ServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddReservoirDevTools());
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
        services.AddReservoir();
        services.AddReservoirDevTools();
        services.AddSingleton(new Mock<IJSRuntime>().Object);
        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        // Act
        ReservoirDevToolsInterop interop = scope.ServiceProvider.GetRequiredService<ReservoirDevToolsInterop>();

        // Assert
        Assert.NotNull(interop);
    }
}