using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Reservoir.Blazor.L0Tests;

/// <summary>
///     Tests for Reservoir DevTools registrations.
/// </summary>
public sealed class ReservoirDevToolsRegistrationsTests
{
    /// <summary>
    ///     AddReservoirDevTools should replace the IStore registration.
    /// </summary>
    [Fact]
    public void AddReservoirDevToolsReplacesStoreRegistration()
    {
        // Arrange
        ServiceCollection services = [];
        services.AddReservoir();
        services.AddReservoirDevTools(options => options.Enablement = ReservoirDevToolsEnablement.Off);
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

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
