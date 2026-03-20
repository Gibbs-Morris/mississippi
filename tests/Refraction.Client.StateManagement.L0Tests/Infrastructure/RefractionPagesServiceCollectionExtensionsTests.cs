using Microsoft.Extensions.DependencyInjection;

using Mississippi.Refraction.Client.StateManagement.Infrastructure;


namespace Mississippi.Refraction.Client.StateManagement.L0Tests.Infrastructure;

/// <summary>
///     Tests for <see cref="RefractionPagesServiceCollectionExtensions" />.
/// </summary>
public sealed class RefractionPagesServiceCollectionExtensionsTests
{
    /// <summary>
    ///     UseRefractionPages returns the service collection for chaining.
    /// </summary>
    [Fact]
    public void UseRefractionPagesReturnsServiceCollection()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.UseRefractionPages();

        // Assert
        Assert.Same(services, result);
    }
}