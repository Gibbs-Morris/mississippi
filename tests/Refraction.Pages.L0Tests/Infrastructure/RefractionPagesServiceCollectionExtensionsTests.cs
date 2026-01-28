
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Refraction.Pages.Infrastructure;


namespace Mississippi.Refraction.Pages.L0Tests.Infrastructure;

/// <summary>
///     Tests for <see cref="RefractionPagesServiceCollectionExtensions" />.
/// </summary>
public sealed class RefractionPagesServiceCollectionExtensionsTests
{
    /// <summary>
    ///     AddRefractionPages returns the service collection for chaining.
    /// </summary>
    [Fact]
        public void AddRefractionPagesReturnsServiceCollection()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddRefractionPages();

        // Assert
        Assert.Same(services, result);
    }
}