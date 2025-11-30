using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Mississippi.EventSourcing.Serialization.Abstractions;

using Xunit;

namespace Mississippi.EventSourcing.Serialization.Tests;

/// <summary>
///     Tests covering <see cref="SerializationStorageProviderHelpers" /> registrations.
/// </summary>
public sealed class SerializationStorageProviderHelpersTests
{
    /// <summary>
    ///     Ensures registering a provider type wires every serialization interface to the provider implementation and keeps
    ///     the singleton lifetime per interface.
    /// </summary>
    [Fact]
    public void RegisterSerializationStorageProviderShouldRegisterAllInterfacesGivenProviderType()
    {
        ServiceCollection services = new();

        services.RegisterSerializationStorageProvider<TestSerializationProvider>();
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        ISerializationReader reader = serviceProvider.GetRequiredService<ISerializationReader>();
        ISerializationReader secondReader = serviceProvider.GetRequiredService<ISerializationReader>();
        IAsyncSerializationReader asyncReader = serviceProvider.GetRequiredService<IAsyncSerializationReader>();
        IAsyncSerializationReader secondAsyncReader = serviceProvider.GetRequiredService<IAsyncSerializationReader>();
        ISerializationWriter writer = serviceProvider.GetRequiredService<ISerializationWriter>();
        ISerializationWriter secondWriter = serviceProvider.GetRequiredService<ISerializationWriter>();
        IAsyncSerializationWriter asyncWriter = serviceProvider.GetRequiredService<IAsyncSerializationWriter>();
        IAsyncSerializationWriter secondAsyncWriter = serviceProvider.GetRequiredService<IAsyncSerializationWriter>();

        Assert.Same(reader, secondReader);
        Assert.Same(asyncReader, secondAsyncReader);
        Assert.Same(writer, secondWriter);
        Assert.Same(asyncWriter, secondAsyncWriter);

        Assert.IsType<TestSerializationProvider>(reader);
        Assert.IsType<TestSerializationProvider>(asyncReader);
        Assert.IsType<TestSerializationProvider>(writer);
        Assert.IsType<TestSerializationProvider>(asyncWriter);
    }

    /// <summary>
    ///     Ensures the overload accepting an options action configures the expected options instance.
    /// </summary>
    [Fact]
    public void RegisterSerializationStorageProviderWithOptionsActionShouldConfigureOptions()
    {
        ServiceCollection services = new();
        const string expectedName = "configured";

        services.RegisterSerializationStorageProvider<TestSerializationProvider, TestOptions>(options => options.Name = expectedName);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        TestOptions options = serviceProvider.GetRequiredService<IOptions<TestOptions>>().Value;

        Assert.Equal(expectedName, options.Name);
    }

    /// <summary>
    ///     Ensures the configuration-based overload binds the configuration section into the expected options instance.
    /// </summary>
    [Fact]
    public void RegisterSerializationStorageProviderWithConfigurationShouldBindOptions()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>("Name", "configured"),
            })
            .Build();

        services.RegisterSerializationStorageProvider<TestSerializationProvider, TestOptions>(configuration);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        TestOptions options = serviceProvider.GetRequiredService<IOptions<TestOptions>>().Value;

        Assert.Equal("configured", options.Name);
    }

    private sealed class TestOptions
    {
        public string? Name { get; set; }
    }

    private sealed class TestSerializationProvider : ISerializationProvider
    {
        public string Format => "test";

        public ReadOnlyMemory<byte> Write<T>(T value) => ReadOnlyMemory<byte>.Empty;

        public ValueTask WriteAsync<T>(T value, Stream destination, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public T Read<T>(ReadOnlyMemory<byte> payload) => throw new NotImplementedException();

        public ValueTask<T> ReadAsync<T>(Stream source, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(default(T)!);
    }
}
