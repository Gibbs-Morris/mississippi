using System;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Reservoir.Abstractions;


namespace Mississippi.Reservoir.L0Tests;

/// <summary>
///     Tests for <see cref="ReservoirRegistrations" />.
/// </summary>
[AllureParentSuite("Mississippi.Reservoir")]
[AllureSuite("Configuration")]
[AllureSubSuite("ReservoirRegistrations")]
public sealed class ReservoirRegistrationsTests
{
    /// <summary>
    ///     AddReservoir should register IStore as scoped.
    /// </summary>
    [Fact]
    [AllureFeature("Service Registration")]
    public void AddReservoirRegistersIStoreAsScoped()
    {
        // Arrange
        ServiceCollection services = [];

        // Act
        services.AddReservoir();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        IStore store1 = scope.ServiceProvider.GetRequiredService<IStore>();
        IStore store2 = scope.ServiceProvider.GetRequiredService<IStore>();

        // Assert
        Assert.Same(store1, store2);
    }

    /// <summary>
    ///     AddReservoir should throw ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void AddReservoirWithNullServicesThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddReservoir());
    }
}