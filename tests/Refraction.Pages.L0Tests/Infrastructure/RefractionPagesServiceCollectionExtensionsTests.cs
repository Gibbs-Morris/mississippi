using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Refraction.Pages.Infrastructure;


namespace Mississippi.Refraction.Pages.L0Tests.Infrastructure;

/// <summary>
///     Tests for <see cref="RefractionPagesServiceCollectionExtensions" />.
/// </summary>
[AllureSuite("Refraction.Pages")]
[AllureSubSuite("Infrastructure")]
public sealed class RefractionPagesServiceCollectionExtensionsTests
{
    /// <summary>
    ///     AddRefractionPages returns the service collection for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("DI Registration")]
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