using System;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Serialization.Abstractions;
using Mississippi.EventSourcing.Snapshots.Abstractions;

using Moq;


namespace Mississippi.EventSourcing.Snapshots.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotStateConverter{TSnapshot}" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Snapshots")]
[AllureSubSuite("Snapshot State Converter")]
public sealed class SnapshotStateConverterTests
{
    /// <summary>
    ///     Test state for converter tests.
    /// </summary>
    private sealed record TestState
    {
        /// <summary>
        ///     Gets the value.
        /// </summary>
        public int Value { get; init; }
    }

    /// <summary>
    ///     Verifies that FromEnvelope deserializes the state from the envelope.
    /// </summary>
    [Fact]
    [AllureFeature("Deserialization")]
    public void FromEnvelopeDeserializesState()
    {
        // Arrange
        TestState expectedState = new()
        {
            Value = 99,
        };
        byte[] data = [5, 6, 7, 8];
        SnapshotEnvelope envelope = new()
        {
            Data = [.. data],
            DataContentType = "application/json",
            ReducerHash = "hash",
        };
        Mock<ISerializationProvider> serializationProviderMock = new();
        serializationProviderMock.Setup(p => p.Deserialize<TestState>(It.IsAny<ReadOnlyMemory<byte>>()))
            .Returns(expectedState);
        SnapshotStateConverter<TestState> converter = new(serializationProviderMock.Object);

        // Act
        TestState result = converter.FromEnvelope(envelope);

        // Assert
        Assert.Equal(expectedState.Value, result.Value);
        serializationProviderMock.Verify(p => p.Deserialize<TestState>(It.IsAny<ReadOnlyMemory<byte>>()), Times.Once);
    }

    /// <summary>
    ///     Verifies that FromEnvelope throws when the envelope is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void FromEnvelopeThrowsWhenEnvelopeIsNull()
    {
        // Arrange
        Mock<ISerializationProvider> serializationProviderMock = new();
        SnapshotStateConverter<TestState> converter = new(serializationProviderMock.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => converter.FromEnvelope(null!));
    }

    /// <summary>
    ///     Verifies that ToEnvelope serializes the state with the provided reducer hash.
    /// </summary>
    [Fact]
    [AllureFeature("Serialization")]
    public void ToEnvelopeSerializesStateWithReducerHash()
    {
        // Arrange
        const string reducerHash = "test-hash";
        const string format = "application/json";
        TestState state = new()
        {
            Value = 42,
        };
        byte[] expectedData = [1, 2, 3, 4];
        Mock<ISerializationProvider> serializationProviderMock = new();
        serializationProviderMock.Setup(p => p.Format).Returns(format);
        serializationProviderMock.Setup(p => p.Serialize(state)).Returns(new ReadOnlyMemory<byte>(expectedData));
        SnapshotStateConverter<TestState> converter = new(serializationProviderMock.Object);

        // Act
        SnapshotEnvelope result = converter.ToEnvelope(state, reducerHash);

        // Assert
        Assert.Equal(expectedData, result.Data.AsSpan().ToArray());
        Assert.Equal(format, result.DataContentType);
        Assert.Equal(reducerHash, result.ReducerHash);
    }

    /// <summary>
    ///     Verifies that ToEnvelope throws when the reducer hash is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void ToEnvelopeThrowsWhenReducerHashIsNull()
    {
        // Arrange
        Mock<ISerializationProvider> serializationProviderMock = new();
        SnapshotStateConverter<TestState> converter = new(serializationProviderMock.Object);
        TestState state = new()
        {
            Value = 1,
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => converter.ToEnvelope(state, null!));
    }

    /// <summary>
    ///     Verifies that ToEnvelope throws when the state is null.
    /// </summary>
    [Fact]
    [AllureFeature("Validation")]
    public void ToEnvelopeThrowsWhenStateIsNull()
    {
        // Arrange
        Mock<ISerializationProvider> serializationProviderMock = new();
        SnapshotStateConverter<TestState> converter = new(serializationProviderMock.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => converter.ToEnvelope(null!, "hash"));
    }
}