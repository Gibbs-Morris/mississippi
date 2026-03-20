#pragma warning disable CS0618 // Testing legacy composition APIs pending issue #237.
using Microsoft.Extensions.DependencyInjection;

using Mississippi.Brooks.Serialization.Abstractions;


namespace Mississippi.Brooks.Serialization.Json.L0Tests;

/// <summary>
///     Tests for <see cref="ServiceRegistration" /> behavior.
/// </summary>
public sealed class ServiceRegistrationTests
{
    /// <summary>
    ///     AddJsonSerialization should register ISerializationProvider.
    /// </summary>
    [Fact]
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

#pragma warning restore CS0618