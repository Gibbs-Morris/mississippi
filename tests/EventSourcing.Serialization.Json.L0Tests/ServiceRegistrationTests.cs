using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Serialization.Abstractions;


namespace Mississippi.EventSourcing.Serialization.Json.L0Tests;

/// <summary>
///     Tests for <see cref="ServiceRegistration" /> behavior.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Serialization")]
[AllureSubSuite("Service Registration")]
public sealed class ServiceRegistrationTests
{
    /// <summary>
    ///     AddJsonSerialization should register ISerializationProvider.
    /// </summary>
    [Fact]
    [AllureFeature("DI Registration")]
    public void AddJsonSerializationRegistersSerializationProvider()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        services.AddJsonSerialization();

        // Assert
        using ServiceProvider provider = services.BuildServiceProvider();
        ISerializationProvider? serializationProvider = provider.GetService<ISerializationProvider>();
        Assert.NotNull(serializationProvider);
        Assert.IsType<JsonSerializationProvider>(serializationProvider);
    }

    /// <summary>
    ///     AddJsonSerialization should return the service collection for chaining.
    /// </summary>
    [Fact]
    [AllureFeature("DI Registration")]
    public void AddJsonSerializationReturnsServiceCollection()
    {
        // Arrange
        ServiceCollection services = new();

        // Act
        IServiceCollection result = services.AddJsonSerialization();

        // Assert
        Assert.Same(services, result);
    }
}