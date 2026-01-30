using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
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
    ///     AddReservoirDevTools should replace the IStore registration.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test operation.</returns>
    [Fact]
    public async Task AddReservoirDevToolsReplacesStoreRegistrationAsync()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddReservoir();
        services.AddReservoirDevTools(options => options.Enablement = ReservoirDevToolsEnablement.Off);
        services.AddSingleton(new Mock<IJSRuntime>().Object);
        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        // Act
        IStore store = scope.ServiceProvider.GetRequiredService<IStore>();

        // Assert
        Assert.Equal("ReservoirDevToolsStore", store.GetType().Name);
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
}
