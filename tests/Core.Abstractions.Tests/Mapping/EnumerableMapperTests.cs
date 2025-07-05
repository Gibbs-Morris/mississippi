﻿using System.Globalization;

using Mississippi.Core.Abstractions.Mapping;

using Moq;


namespace Mississippi.Core.Abstractions.Tests.Mapping;

/// <summary>
///     Contains unit tests for the <see cref="EnumerableMapper{TFrom,TTo}" /> class.
/// </summary>
public class EnumerableMapperTests
{
    /// <summary>
    ///     Tests that Projection collection of integers is correctly mapped to Projection collection of strings.
    /// </summary>
    [Fact]
    public void MapsCollectionCorrectly()
    {
        // Arrange
        Mock<IMapper<int, string>> mockMapper = new();
        mockMapper.Setup(m => m.Map(It.IsAny<int>())).Returns<int>(i => i.ToString(CultureInfo.InvariantCulture));
        EnumerableMapper<int, string> enumerableMapper = new(mockMapper.Object);
        List<int> input = new()
        {
            1,
            2,
            3,
        };

        // Act
        IEnumerable<string> result = enumerableMapper.Map(input);

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
    ///     Tests that an empty collection is correctly mapped to an empty collection.
    /// </summary>
    [Fact]
    public void MapsEmptyCollection()
    {
        // Arrange
        Mock<IMapper<int, string>> mockMapper = new();
        EnumerableMapper<int, string> enumerableMapper = new(mockMapper.Object);

        // ReSharper disable once CollectionNeverUpdated.Local
        List<int> input = [];

        // Act
        IEnumerable<string> result = enumerableMapper.Map(input);

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    ///     Tests that passing Projection null value to the Map function throws an ArgumentNullException.
    /// </summary>
    [Fact]
    public void MapNullInputThrowsArgumentNullException()
    {
        // Arrange
        Mock<IMapper<int, string>> mockMapper = new();
        EnumerableMapper<int, string> enumerableMapper = new(mockMapper.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => enumerableMapper.Map(null!));
    }

    /// <summary>
    ///     Tests that Projection collection containing nullable integers, including null elements,
    ///     is correctly mapped to Projection collection of strings.
    /// </summary>
    [Fact]
    public void MapsCollectionWithNullElements()
    {
        // Arrange
        Mock<IMapper<int?, string>> mockMapper = new();
        mockMapper.Setup(m => m.Map(It.IsAny<int?>()))
            .Returns<int?>(i => i?.ToString(CultureInfo.InvariantCulture) ?? "null");
        EnumerableMapper<int?, string> enumerableMapper = new(mockMapper.Object);
        List<int?> input = new()
        {
            1,
            null,
            3,
        };

        // Act
        IEnumerable<string> result = enumerableMapper.Map(input);

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
}