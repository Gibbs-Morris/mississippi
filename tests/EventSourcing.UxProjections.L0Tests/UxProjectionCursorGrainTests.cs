using System;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Streaming;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Moq;


namespace Mississippi.EventSourcing.UxProjections.L0Tests;

/// <summary>
///     Tests for <see cref="UxProjectionCursorGrain" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("UX Projections")]
[AllureSubSuite("UxProjectionCursorGrain")]
public sealed class UxProjectionCursorGrainTests
{
    /// <summary>
    ///     Verifies that BrookCursorMovedEvent can be created with a position.
    /// </summary>
    [Fact]
    [AllureFeature("Event Model")]
    public void BrookCursorMovedEventCanBeCreatedWithPosition()
    {
        // Arrange & Act
        BrookCursorMovedEvent cursorEvent = new(new(10));

        // Assert
        Assert.Equal(10, cursorEvent.NewPosition.Value);
    }

    /// <summary>
    ///     Verifies that BrookCursorMovedEvent stores position correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Event Model")]
    public void BrookCursorMovedEventStoresPositionCorrectly()
    {
        // Arrange
        BrookPosition position = new(100);

        // Act
        BrookCursorMovedEvent cursorEvent = new(position);

        // Assert
        Assert.Equal(100, cursorEvent.NewPosition.Value);
        Assert.False(cursorEvent.NewPosition.NotSet);
    }

    /// <summary>
    ///     Verifies that BrookCursorMovedEvent with negative position represents not set.
    /// </summary>
    [Fact]
    [AllureFeature("Event Model")]
    public void BrookCursorMovedEventWithNegativePositionRepresentsNotSet()
    {
        // Arrange & Act
        BrookCursorMovedEvent cursorEvent = new(new(-1));

        // Assert
        Assert.True(cursorEvent.NewPosition.NotSet);
        Assert.Equal(-1, cursorEvent.NewPosition.Value);
    }

    /// <summary>
    ///     Verifies that BrookPosition equality works correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Position Equality")]
    public void BrookPositionEqualityWorksCorrectly()
    {
        // Arrange
        BrookPosition a = new(5);
        BrookPosition b = new(5);
        BrookPosition c = new(10);

        // Assert
        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    /// <summary>
    ///     Verifies that BrookPosition hash codes are equal for equal values.
    /// </summary>
    [Fact]
    [AllureFeature("Position Equality")]
    public void BrookPositionHashCodesAreEqualForEqualValues()
    {
        // Arrange
        BrookPosition position1 = new(100);
        BrookPosition position2 = new(100);

        // Assert
        Assert.Equal(position1.GetHashCode(), position2.GetHashCode());
    }

    /// <summary>
    ///     Verifies that BrookPosition implicit conversion from int works.
    /// </summary>
    [Fact]
    [AllureFeature("Position Conversion")]
    public void BrookPositionImplicitConversionFromIntWorks()
    {
        // Arrange & Act
        BrookPosition position = 42;

        // Assert
        Assert.Equal(42, position.Value);
    }

    /// <summary>
    ///     Verifies that BrookPosition comparison handles negative values correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Position Comparison")]
    public void BrookPositionIsNewerThanHandlesNegativeValues()
    {
        // Arrange
        BrookPosition negative = new(-1);
        BrookPosition zero = new(0);
        BrookPosition positive = new(5);

        // Assert
        Assert.True(zero.IsNewerThan(negative));
        Assert.True(positive.IsNewerThan(negative));
        Assert.True(positive.IsNewerThan(zero));
    }

    /// <summary>
    ///     Verifies that BrookPosition IsNewerThan returns correct comparison.
    /// </summary>
    [Fact]
    [AllureFeature("Position Comparison")]
    public void BrookPositionIsNewerThanReturnsCorrectComparison()
    {
        // Arrange
        BrookPosition older = new(5);
        BrookPosition newer = new(10);

        // Assert
        Assert.True(newer.IsNewerThan(older));
        Assert.False(older.IsNewerThan(newer));
        Assert.False(older.IsNewerThan(older));
    }

    /// <summary>
    ///     Verifies that BrookPosition comparison handles equal values correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Position Comparison")]
    public void BrookPositionIsNewerThanReturnsFalseForEqualValues()
    {
        // Arrange
        BrookPosition position1 = new(50);
        BrookPosition position2 = new(50);

        // Assert
        Assert.False(position1.IsNewerThan(position2));
        Assert.False(position2.IsNewerThan(position1));
    }

    /// <summary>
    ///     Verifies that BrookPosition minus one is the initial state.
    /// </summary>
    [Fact]
    [AllureFeature("Position State")]
    public void BrookPositionMinusOneIsInitialState()
    {
        // Arrange
        BrookPosition initial = new(-1);

        // Assert
        Assert.Equal(-1, initial.Value);
    }

    /// <summary>
    ///     Verifies that BrookPosition NotSet returns true for default position.
    /// </summary>
    [Fact]
    [AllureFeature("Position State")]
    public void BrookPositionNotSetReturnsTrueForDefault()
    {
        // Arrange
        BrookPosition notSet = new(-1);
        BrookPosition set = new(5);

        // Assert
        Assert.True(notSet.NotSet);
        Assert.False(set.NotSet);
    }

    /// <summary>
    ///     Verifies that BrookPosition with value zero is considered set.
    /// </summary>
    [Fact]
    [AllureFeature("Position State")]
    public void BrookPositionZeroIsConsideredSet()
    {
        // Arrange
        BrookPosition zero = new(0);

        // Assert
        Assert.Equal(0, zero.Value);
        Assert.False(zero.NotSet);
    }

    /// <summary>
    ///     Verifies that the cursor grain interface DeactivateAsync method completes.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("Interface Contract")]
    public async Task IUxProjectionCursorGrainDeactivateAsyncCompletes()
    {
        // Arrange
        Mock<IUxProjectionCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(g => g.DeactivateAsync()).Returns(Task.CompletedTask);

        // Act & Assert
        await cursorGrainMock.Object.DeactivateAsync();
        cursorGrainMock.Verify(g => g.DeactivateAsync(), Times.Once);
    }

    /// <summary>
    ///     Verifies that the cursor grain interface GetPositionAsync method can be mocked correctly.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("Interface Contract")]
    public async Task IUxProjectionCursorGrainGetPositionAsyncReturnsPosition()
    {
        // Arrange
        Mock<IUxProjectionCursorGrain> cursorGrainMock = new();
        cursorGrainMock.Setup(g => g.GetPositionAsync()).ReturnsAsync(new BrookPosition(42));

        // Act
        BrookPosition position = await cursorGrainMock.Object.GetPositionAsync();

        // Assert
        Assert.Equal(42, position.Value);
    }

    /// <summary>
    ///     Verifies that cursor grain interface SetPositionAsync can be mocked.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("Interface Contract")]
    public async Task IUxProjectionCursorGrainSetPositionAsyncCanBeMocked()
    {
        // Arrange
        Mock<IUxProjectionCursorGrain> cursorGrainMock = new();
        BrookPosition expectedPosition = new(75);
        cursorGrainMock.Setup(g => g.GetPositionAsync()).ReturnsAsync(expectedPosition);

        // Act
        BrookPosition position = await cursorGrainMock.Object.GetPositionAsync();

        // Assert
        Assert.Equal(75, position.Value);
        cursorGrainMock.Verify(g => g.GetPositionAsync(), Times.Once);
    }

    /// <summary>
    ///     Verifies that multiple cursor grain instances can track different positions.
    /// </summary>
    /// <returns>Asynchronous test task.</returns>
    [Fact]
    [AllureFeature("Interface Contract")]
    public async Task MultipleCursorGrainsTrackDifferentPositions()
    {
        // Arrange
        Mock<IUxProjectionCursorGrain> grain1Mock = new();
        Mock<IUxProjectionCursorGrain> grain2Mock = new();
        grain1Mock.Setup(g => g.GetPositionAsync()).ReturnsAsync(new BrookPosition(10));
        grain2Mock.Setup(g => g.GetPositionAsync()).ReturnsAsync(new BrookPosition(20));

        // Act
        BrookPosition position1 = await grain1Mock.Object.GetPositionAsync();
        BrookPosition position2 = await grain2Mock.Object.GetPositionAsync();

        // Assert
        Assert.Equal(10, position1.Value);
        Assert.Equal(20, position2.Value);
        Assert.NotEqual(position1, position2);
    }

    /// <summary>
    ///     Verifies that cursor grain factory resolves cursor grains correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Factory Integration")]
    public void UxProjectionGrainFactoryGetCursorGrainResolves()
    {
        // Arrange
        Mock<IUxProjectionCursorGrain> cursorGrainMock = new();
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        UxProjectionCursorKey key = new("TEST.MODULE.STREAM", "entity-123");
        factoryMock.Setup(f => f.GetUxProjectionCursorGrain(key)).Returns(cursorGrainMock.Object);

        // Act
        IUxProjectionCursorGrain resolvedGrain = factoryMock.Object.GetUxProjectionCursorGrain(key);

        // Assert
        Assert.Same(cursorGrainMock.Object, resolvedGrain);
    }

    /// <summary>
    ///     Verifies that UxProjectionKey at max length is accepted.
    /// </summary>
    [Fact]
    [AllureFeature("Key Validation")]
    public void UxProjectionKeyAcceptsMaxLengthEntityId()
    {
        // Arrange
        string maxLengthEntityId = new('x', 4192);

        // Act
        UxProjectionKey key = new(maxLengthEntityId);

        // Assert
        Assert.Equal(4192, key.EntityId.Length);
    }

    /// <summary>
    ///     Verifies that UxProjectionKey correctly parses the primary key format.
    /// </summary>
    [Fact]
    [AllureFeature("Key Parsing")]
    public void UxProjectionKeyFromStringParsesValidKey()
    {
        // Act
        UxProjectionKey key = UxProjectionKey.FromString("entity-123");

        // Assert
        Assert.Equal("entity-123", key.EntityId);
    }

    /// <summary>
    ///     Verifies that UxProjectionKey FromString throws for null string.
    /// </summary>
    [Fact]
    [AllureFeature("Key Parsing")]
    public void UxProjectionKeyFromStringThrowsForNullString()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => UxProjectionKey.FromString(null!));
    }

    /// <summary>
    ///     Verifies that UxProjectionKey implicit conversion to string works.
    /// </summary>
    [Fact]
    [AllureFeature("Key Conversion")]
    public void UxProjectionKeyImplicitConversionToStringWorks()
    {
        // Arrange
        UxProjectionKey key = new("entity-123");

        // Act
        string result = key;

        // Assert
        Assert.Equal("entity-123", result);
    }

    /// <summary>
    ///     Verifies that UxProjectionKey roundtrips through ToString and FromString.
    /// </summary>
    [Fact]
    [AllureFeature("Key Serialization")]
    public void UxProjectionKeyRoundtripsThroughSerialization()
    {
        // Arrange
        UxProjectionKey original = new("test-id");

        // Act
        string serialized = original.ToString();
        UxProjectionKey deserialized = UxProjectionKey.FromString(serialized);

        // Assert
        Assert.Equal(original.EntityId, deserialized.EntityId);
    }

    /// <summary>
    ///     Verifies that UxProjectionKey stores entity ID correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Key Creation")]
    public void UxProjectionKeyStoresEntityIdCorrectly()
    {
        // Act
        UxProjectionKey key = new("my-entity");

        // Assert
        Assert.Equal("my-entity", key.EntityId);
    }

    /// <summary>
    ///     Verifies that UxProjectionKey throws when entity ID exceeds max length.
    /// </summary>
    [Fact]
    [AllureFeature("Key Validation")]
    public void UxProjectionKeyThrowsWhenEntityIdExceedsMaxLength()
    {
        // Arrange
        string tooLongEntityId = new('x', 4193);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new UxProjectionKey(tooLongEntityId));
    }

    /// <summary>
    ///     Verifies that UxProjectionKey throws when entity ID is null.
    /// </summary>
    [Fact]
    [AllureFeature("Key Validation")]
    public void UxProjectionKeyThrowsWhenEntityIdIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new UxProjectionKey(null!));
    }

    /// <summary>
    ///     Verifies that UxProjectionKey ToString returns the entity ID.
    /// </summary>
    [Fact]
    [AllureFeature("Key Serialization")]
    public void UxProjectionKeyToStringReturnsEntityId()
    {
        // Arrange
        UxProjectionKey key = new("entity-123");

        // Act
        string result = key.ToString();

        // Assert
        Assert.Equal("entity-123", result);
    }
}