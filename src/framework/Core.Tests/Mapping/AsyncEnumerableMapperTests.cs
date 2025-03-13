using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Mississippi.Core.Abstractions.Mapping;
using Moq;

namespace Mississippi.Core.Tests.Mapping;

/// <summary>
///     Provides unit tests for the <see cref="AsyncEnumerableMapper{TFrom,TTo}" /> class.
/// </summary>
public class AsyncEnumerableMapperTests
{
    /// <summary>
    ///     Tests that a non-empty asynchronous collection is mapped correctly.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task MapsAsyncCollectionCorrectlyAsync()
    {
        // Arrange
        Mock<IMapper<int, string>> mockMapper = new();
        mockMapper.Setup(m => m.Map(It.IsAny<int>())).Returns<int>(i => i.ToString(CultureInfo.InvariantCulture));
        AsyncEnumerableMapper<int, string> asyncEnumerableMapper = new(mockMapper.Object);
        IAsyncEnumerable<int> input = GetAsyncEnumerableAsync(
            new List<int>
            {
                1,
                2,
                3,
            });

        // Act
        List<string> result = await asyncEnumerableMapper.Map(input).ToListAsync();

        // Assert
        Assert.Equal(
            new List<string>
            {
                "1",
                "2",
                "3",
            },
            result);
    }

    /// <summary>
    ///     Tests that an empty asynchronous collection is mapped to an empty collection.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task MapsEmptyAsyncCollectionAsync()
    {
        // Arrange
        Mock<IMapper<int, string>> mockMapper = new();
        AsyncEnumerableMapper<int, string> asyncEnumerableMapper = new(mockMapper.Object);
        IAsyncEnumerable<int> input = GetAsyncEnumerableAsync(new List<int>());

        // Act
        List<string> result = await asyncEnumerableMapper.Map(input).ToListAsync();

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    ///     Tests that passing a null value to the Map function throws an ArgumentNullException.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task MapNullInputThrowsArgumentNullExceptionAsync()
    {
        // Arrange
        Mock<IMapper<int, string>> mockMapper = new();
        AsyncEnumerableMapper<int, string> asyncEnumerableMapper = new(mockMapper.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await asyncEnumerableMapper.Map(null!).ToListAsync());
    }

    /// <summary>
    ///     Tests that an asynchronous collection with null elements is mapped correctly, with null elements represented as
    ///     "null".
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task MapsAsyncCollectionWithNullElementsAsync()
    {
        // Arrange
        Mock<IMapper<int?, string>> mockMapper = new();
        mockMapper.Setup(m => m.Map(It.IsAny<int?>()))
            .Returns<int?>(i => i?.ToString(CultureInfo.InvariantCulture) ?? "null");
        AsyncEnumerableMapper<int?, string> asyncEnumerableMapper = new(mockMapper.Object);
        IAsyncEnumerable<int?> input = GetAsyncEnumerableAsync(
            new List<int?>
            {
                1,
                null,
                3,
            });

        // Act
        List<string> result = await asyncEnumerableMapper.Map(input).ToListAsync();

        // Assert
        Assert.Equal(
            new List<string>
            {
                "1",
                "null",
                "3",
            },
            result);
    }

    /// <summary>
    ///     Asynchronously enumerates a collection of items.
    /// </summary>
    /// <typeparam name="T">The type of the items in the collection.</typeparam>
    /// <param name="items">The collection of items to enumerate.</param>
    /// <returns>An asynchronous enumerable of the items.</returns>
    [SuppressMessage(
        "Major Code Smell",
        "S4456:Parameter validation in yielding methods should be wrapped",
        Justification = "Required for IAsyncEnumerable.")]
    private static async IAsyncEnumerable<T> GetAsyncEnumerableAsync<T>(
        IEnumerable<T> items
    )
    {
        ArgumentNullException.ThrowIfNull(items);
        foreach (T item in items)
        {
            yield return item;
            await Task.Yield();
        }
    }
}