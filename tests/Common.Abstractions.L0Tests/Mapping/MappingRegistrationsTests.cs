using System.Globalization;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Common.Abstractions.Mapping;


namespace Mississippi.Common.Abstractions.L0Tests.Mapping;

/// <summary>
///     Contains unit tests that verify the <see cref="MappingRegistrations" /> extension methods register expected
///     mappers.
/// </summary>
public sealed class MappingRegistrationsTests
{
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

    /// <summary>
    ///     Verifies that <see cref="MappingRegistrations.AddIAsyncEnumerableMapper(IServiceCollection)" /> registers the async
    ///     enumerable mapper implementation.
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
    ///     Verifies that <see cref="MappingRegistrations.AddIEnumerableMapper(IServiceCollection)" /> registers the enumerable
    ///     mapper implementation.
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
}