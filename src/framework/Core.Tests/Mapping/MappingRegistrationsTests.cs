using System.Globalization;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Core.Abstractions.Mapping;


namespace Mississippi.Core.Tests.Mapping;

/// <summary>
///     Contains unit tests for verifying the registration of mappers in the service collection.
/// </summary>
public class MappingRegistrationsTests
{
    /// <summary>
    ///     Tests if a single mapper is correctly added to the service collection.
    /// </summary>
    [Fact]
    public void AddsMapperToServiceCollection()
    {
        ServiceCollection services = new();
        services.AddMapper<int, string, MockMapper>();
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IMapper<int, string>? mapper = serviceProvider.GetService<IMapper<int, string>>();
        Assert.NotNull(mapper);
        Assert.IsType<MockMapper>(mapper);
    }

    /// <summary>
    ///     Tests if an IEnumerable mapper is correctly added to the service collection.
    /// </summary>
    [Fact]
    public void AddsIEnumerableMapperToServiceCollection()
    {
        ServiceCollection services = new();
        services.AddMapper<int, string, MockMapper>();
        services.AddIEnumerableMapper();
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IEnumerableMapper<int, string>? mapper = serviceProvider.GetService<IEnumerableMapper<int, string>>();
        Assert.NotNull(mapper);
        Assert.IsType<EnumerableMapper<int, string>>(mapper);
    }

    /// <summary>
    ///     Tests if an IAsyncEnumerable mapper is correctly added to the service collection.
    /// </summary>
    [Fact]
    public void AddsIAsyncEnumerableMapperToServiceCollection()
    {
        ServiceCollection services = new();
        services.AddMapper<int, string, MockMapper>();
        services.AddIAsyncEnumerableMapper();
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IAsyncEnumerableMapper<int, string>? mapper = serviceProvider.GetService<IAsyncEnumerableMapper<int, string>>();
        Assert.NotNull(mapper);
        Assert.IsType<AsyncEnumerableMapper<int, string>>(mapper);
    }

    /// <summary>
    ///     A mock implementation of the <see cref="IMapper{TFrom, TTo}" /> interface
    ///     for testing purposes.
    /// </summary>
    private class MockMapper : IMapper<int, string>
    {
        /// <summary>
        ///     Maps an integer to its string representation.
        /// </summary>
        /// <param name="source">The integer to map.</param>
        /// <returns>The string representation of the integer.</returns>
        public string Map(
            int source
        ) =>
            source.ToString(CultureInfo.InvariantCulture);
    }
}