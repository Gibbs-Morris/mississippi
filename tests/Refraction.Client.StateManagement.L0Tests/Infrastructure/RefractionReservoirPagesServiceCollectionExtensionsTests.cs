using Microsoft.Extensions.DependencyInjection;

using Mississippi.Refraction.Client.StateManagement.Infrastructure;


namespace Mississippi.Refraction.Client.StateManagement.L0Tests.Infrastructure;

/// <summary>
///     Tests for <see cref="RefractionReservoirPagesServiceCollectionExtensions" />.
/// </summary>
public sealed class RefractionReservoirPagesServiceCollectionExtensionsTests
{
    /// <summary>
    ///     AddRefractionReservoirPages returns the service collection for chaining.
    /// </summary>
    [Fact]
    public void AddRefractionReservoirPagesReturnsServiceCollection()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddRefractionReservoirPages();

        // Assert
        Assert.Same(services, result);
    }
}