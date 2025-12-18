using System;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Brooks.Cursor;
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
    private const string ValidPrimaryKey = "TestProjection|TEST.MODULE.STREAM|entity-123";

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
    ///     Verifies that cursor grain factory resolves cursor grains correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Factory Integration")]
    public void UxProjectionGrainFactoryGetCursorGrainResolves()
    {
        // Arrange
        Mock<IUxProjectionCursorGrain> cursorGrainMock = new();
        Mock<IUxProjectionGrainFactory> factoryMock = new();
        UxProjectionKey key = UxProjectionKey.For<TestProjection, TestBrookDefinition>("entity-123");
        factoryMock.Setup(f => f.GetUxProjectionCursorGrain(key)).Returns(cursorGrainMock.Object);

        // Act
        IUxProjectionCursorGrain resolvedGrain = factoryMock.Object.GetUxProjectionCursorGrain(key);

        // Assert
        Assert.Same(cursorGrainMock.Object, resolvedGrain);
    }

    /// <summary>
    ///     Verifies that UxProjectionKey correctly parses the primary key format.
    /// </summary>
    [Fact]
    [AllureFeature("Key Parsing")]
    public void UxProjectionKeyFromStringParsesValidKey()
    {
        // Act
        UxProjectionKey key = UxProjectionKey.FromString(ValidPrimaryKey);

        // Assert
        Assert.Equal("TestProjection", key.ProjectionTypeName);
        Assert.Equal("TEST.MODULE.STREAM", key.BrookKey.Type);
        Assert.Equal("entity-123", key.BrookKey.Id);
    }

    /// <summary>
    ///     Verifies that UxProjectionKey throws when given an invalid key format.
    /// </summary>
    [Fact]
    [AllureFeature("Key Parsing")]
    public void UxProjectionKeyFromStringThrowsForInvalidFormat()
    {
        // Arrange
        const string invalidKey = "invalid-key-without-pipe";

        // Act & Assert
        Assert.Throws<FormatException>(() => UxProjectionKey.FromString(invalidKey));
    }

    /// <summary>
    ///     Verifies that UxProjectionKey ToString returns the expected format.
    /// </summary>
    [Fact]
    [AllureFeature("Key Serialization")]
    public void UxProjectionKeyToStringReturnsCorrectFormat()
    {
        // Arrange
        UxProjectionKey key = UxProjectionKey.For<TestProjection, TestBrookDefinition>("entity-123");

        // Act
        string result = key.ToString();

        // Assert
        Assert.Equal("TestProjection|TEST.MODULE.STREAM|entity-123", result);
    }
}