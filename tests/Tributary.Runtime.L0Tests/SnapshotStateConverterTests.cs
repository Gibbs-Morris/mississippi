using System;

using Mississippi.Brooks.Serialization.Abstractions;
using Mississippi.Tributary.Abstractions;

using Moq;


namespace Mississippi.Tributary.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotStateConverter{TSnapshot}" />.
/// </summary>
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
    public void FromEnvelopeThrowsWhenEnvelopeIsNull()
    {
        // Arrange
        Mock<ISerializationProvider> serializationProviderMock = new();
        SnapshotStateConverter<TestState> converter = new(serializationProviderMock.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => converter.FromEnvelope(null!));
    }

    /// <summary>
    ///     Verifies that FromEnvelope fails when the persisted serializer identity is not registered.
    /// </summary>
    [Fact]
    public void FromEnvelopeThrowsWhenPersistedSerializerIdentityIsUnknown()
    {
        PrimaryTestSnapshotSerializationProvider defaultProvider = new("application/json");
        SnapshotEnvelope envelope = new()
        {
            Data = [5, 6, 7, 8],
            DataContentType = "application/custom-json",
            PayloadSerializerId = typeof(AlternateTestSnapshotSerializationProvider).FullName ??
                                  nameof(AlternateTestSnapshotSerializationProvider),
            ReducerHash = "hash",
        };
        SnapshotStateConverter<TestState> converter = new(defaultProvider, [defaultProvider]);
        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(() => converter.FromEnvelope(envelope));
        Assert.Contains("persisted snapshot serializer id", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies that FromEnvelope honors a persisted concrete serializer identity when one is present.
    /// </summary>
    [Fact]
    public void FromEnvelopeUsesPersistedSerializerIdentityWhenPresent()
    {
        TestState expectedState = new()
        {
            Value = 123,
        };
        PrimaryTestSnapshotSerializationProvider defaultProvider = new("application/json")
        {
            DeserializedValue = new TestState
            {
                Value = 1,
            },
        };
        AlternateTestSnapshotSerializationProvider persistedProvider = new("application/custom-json")
        {
            DeserializedValue = expectedState,
        };
        SnapshotEnvelope envelope = new()
        {
            Data = [5, 6, 7, 8],
            DataContentType = "application/custom-json",
            PayloadSerializerId = persistedProvider.SerializerId,
            ReducerHash = "hash",
        };
        SnapshotStateConverter<TestState> converter = new(defaultProvider, [defaultProvider, persistedProvider]);
        TestState result = converter.FromEnvelope(envelope);
        Assert.Equal(expectedState.Value, result.Value);
        Assert.Equal(0, defaultProvider.DeserializeCallCount);
        Assert.Equal(1, persistedProvider.DeserializeCallCount);
    }

    /// <summary>
    ///     Verifies that ToEnvelope serializes the state with the provided event reducer hash.
    /// </summary>
    [Fact]
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
    ///     Verifies that ToEnvelope throws when the event reducer hash is null.
    /// </summary>
    [Fact]
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
    public void ToEnvelopeThrowsWhenStateIsNull()
    {
        // Arrange
        Mock<ISerializationProvider> serializationProviderMock = new();
        SnapshotStateConverter<TestState> converter = new(serializationProviderMock.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => converter.ToEnvelope(null!, "hash"));
    }
}