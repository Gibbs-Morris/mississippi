using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.EventSourcing.Serialization.Abstractions;

using Moq;


namespace Mississippi.EventSourcing.Aggregates.L0Tests;

/// <summary>
///     Tests for <see cref="BrookEventConverter" />.
/// </summary>
public class BrookEventConverterTests
{
    /// <summary>
    ///     Sets up the non-generic Deserialize method on a mock.
    ///     This helper isolates the CA2263 suppression required because the
    ///     <see cref="BrookEventConverter" /> must use the runtime-type overload.
    /// </summary>
    /// <param name="mock">The serialization provider mock.</param>
    /// <param name="type">The type to match on setup.</param>
    /// <param name="result">The object to return.</param>
#pragma warning disable CA2263 // Prefer generic overload - non-generic is required for runtime type deserialization
    private static void SetupNonGenericDeserialize(
        Mock<ISerializationProvider> mock,
        Type type,
        object result
    ) =>
        mock.Setup(s => s.Deserialize(type, It.IsAny<ReadOnlyMemory<byte>>())).Returns(result);
#pragma warning restore CA2263

    /// <summary>
    ///     Test event record for converter tests.
    /// </summary>
    private sealed record TestEvent(string Value);

    /// <summary>
    ///     Constructor should throw when event type registry is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenEventTypeRegistryIsNull()
    {
        Mock<ISerializationProvider> serializationProviderMock = new();
        Assert.Throws<ArgumentNullException>(() => new BrookEventConverter(serializationProviderMock.Object, null!));
    }

    /// <summary>
    ///     Constructor should throw when serialization provider is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenSerializationProviderIsNull()
    {
        Mock<IEventTypeRegistry> eventTypeRegistryMock = new();
        Assert.Throws<ArgumentNullException>(() => new BrookEventConverter(null!, eventTypeRegistryMock.Object));
    }

    /// <summary>
    ///     ToDomainEvent should deserialize brook event to domain event.
    /// </summary>
    [Fact]
    public void ToDomainEventDeserializesBrookEvent()
    {
        Mock<ISerializationProvider> serializationProviderMock = new();
        Mock<IEventTypeRegistry> eventTypeRegistryMock = new();
        TestEvent expectedEvent = new("test-value");
        eventTypeRegistryMock.Setup(r => r.ResolveType("TEST.EVENT.V1")).Returns(typeof(TestEvent));
        SetupNonGenericDeserialize(serializationProviderMock, typeof(TestEvent), expectedEvent);
        BrookEventConverter converter = new(serializationProviderMock.Object, eventTypeRegistryMock.Object);
        BrookEvent brookEvent = new()
        {
            Id = "e1",
            EventType = "TEST.EVENT.V1",
            Source = "test-brook",
            Data = new byte[] { 1, 2, 3 }.ToImmutableArray(),
            DataContentType = "application/json",
        };
        object result = converter.ToDomainEvent(brookEvent);
        Assert.Same(expectedEvent, result);
    }

    /// <summary>
    ///     ToDomainEvent should throw when brook event is null.
    /// </summary>
    [Fact]
    public void ToDomainEventThrowsWhenBrookEventIsNull()
    {
        Mock<ISerializationProvider> serializationProviderMock = new();
        Mock<IEventTypeRegistry> eventTypeRegistryMock = new();
        BrookEventConverter converter = new(serializationProviderMock.Object, eventTypeRegistryMock.Object);
        Assert.Throws<ArgumentNullException>(() => converter.ToDomainEvent(null!));
    }

    /// <summary>
    ///     ToDomainEvent should throw when event type cannot be resolved.
    /// </summary>
    [Fact]
    public void ToDomainEventThrowsWhenEventTypeCannotBeResolved()
    {
        Mock<ISerializationProvider> serializationProviderMock = new();
        Mock<IEventTypeRegistry> eventTypeRegistryMock = new();
        eventTypeRegistryMock.Setup(r => r.ResolveType("UNKNOWN.EVENT")).Returns((Type?)null);
        BrookEventConverter converter = new(serializationProviderMock.Object, eventTypeRegistryMock.Object);
        BrookEvent brookEvent = new()
        {
            Id = "e1",
            EventType = "UNKNOWN.EVENT",
            Source = "test-brook",
            Data = new byte[] { 1, 2, 3 }.ToImmutableArray(),
        };
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            converter.ToDomainEvent(brookEvent));
        Assert.Contains("UNKNOWN.EVENT", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     ToStorageEvents should convert domain events to brook events.
    /// </summary>
    [Fact]
    public void ToStorageEventsConvertsDomainEventsToBrookEvents()
    {
        Mock<ISerializationProvider> serializationProviderMock = new();
        Mock<IEventTypeRegistry> eventTypeRegistryMock = new();
        byte[] serializedData = { 1, 2, 3 };
        eventTypeRegistryMock.Setup(r => r.ResolveName(typeof(TestEvent))).Returns("TEST.EVENT.V1");
        serializationProviderMock.Setup(s => s.Serialize(It.IsAny<object>())).Returns(serializedData.AsMemory());
        serializationProviderMock.Setup(s => s.Format).Returns("application/json");
        BrookEventConverter converter = new(serializationProviderMock.Object, eventTypeRegistryMock.Object);
        BrookKey source = BrookKey.FromString("TEST.BROOK|entity-1");
        List<object> domainEvents = new()
        {
            new TestEvent("value1"),
            new TestEvent("value2"),
        };
        ImmutableArray<BrookEvent> result = converter.ToStorageEvents(source, domainEvents);
        Assert.Equal(2, result.Length);
        Assert.All(
            result,
            e =>
            {
                Assert.Equal("TEST.EVENT.V1", e.EventType);
                Assert.Equal("TEST.BROOK|entity-1", e.Source);
                Assert.Equal("application/json", e.DataContentType);
                Assert.Equal(serializedData, e.Data.AsSpan().ToArray());
                Assert.NotEmpty(e.Id);
            });
    }

    /// <summary>
    ///     ToStorageEvents should generate unique IDs for each event.
    /// </summary>
    [Fact]
    public void ToStorageEventsGeneratesUniqueIdsForEachEvent()
    {
        Mock<ISerializationProvider> serializationProviderMock = new();
        Mock<IEventTypeRegistry> eventTypeRegistryMock = new();
        eventTypeRegistryMock.Setup(r => r.ResolveName(typeof(TestEvent))).Returns("TEST.EVENT.V1");
        serializationProviderMock.Setup(s => s.Serialize(It.IsAny<object>())).Returns(new byte[] { 1 }.AsMemory());
        serializationProviderMock.Setup(s => s.Format).Returns("json");
        BrookEventConverter converter = new(serializationProviderMock.Object, eventTypeRegistryMock.Object);
        BrookKey source = BrookKey.FromString("TEST.BROOK|entity-1");
        List<object> domainEvents = new()
        {
            new TestEvent("value1"),
            new TestEvent("value2"),
            new TestEvent("value3"),
        };
        ImmutableArray<BrookEvent> result = converter.ToStorageEvents(source, domainEvents);
        HashSet<string> ids = new(result.Select(e => e.Id));
        Assert.Equal(3, ids.Count);
    }

    /// <summary>
    ///     ToStorageEvents should return empty array when domain events is empty.
    /// </summary>
    [Fact]
    public void ToStorageEventsReturnsEmptyArrayWhenDomainEventsIsEmpty()
    {
        Mock<ISerializationProvider> serializationProviderMock = new();
        Mock<IEventTypeRegistry> eventTypeRegistryMock = new();
        BrookEventConverter converter = new(serializationProviderMock.Object, eventTypeRegistryMock.Object);
        BrookKey source = BrookKey.FromString("TEST.BROOK|entity-1");
        ImmutableArray<BrookEvent> result = converter.ToStorageEvents(source, Array.Empty<object>());
        Assert.Empty(result);
    }

    /// <summary>
    ///     ToStorageEvents should throw when domain events is null.
    /// </summary>
    [Fact]
    public void ToStorageEventsThrowsWhenDomainEventsIsNull()
    {
        Mock<ISerializationProvider> serializationProviderMock = new();
        Mock<IEventTypeRegistry> eventTypeRegistryMock = new();
        BrookEventConverter converter = new(serializationProviderMock.Object, eventTypeRegistryMock.Object);
        BrookKey source = BrookKey.FromString("TEST.BROOK|entity-1");
        Assert.Throws<ArgumentNullException>(() => converter.ToStorageEvents(source, null!));
    }

    /// <summary>
    ///     ToStorageEvents should throw when event type is not registered.
    /// </summary>
    [Fact]
    public void ToStorageEventsThrowsWhenEventTypeNotRegistered()
    {
        Mock<ISerializationProvider> serializationProviderMock = new();
        Mock<IEventTypeRegistry> eventTypeRegistryMock = new();
        eventTypeRegistryMock.Setup(r => r.ResolveName(typeof(TestEvent))).Returns((string?)null);
        BrookEventConverter converter = new(serializationProviderMock.Object, eventTypeRegistryMock.Object);
        BrookKey source = BrookKey.FromString("TEST.BROOK|entity-1");
        List<object> domainEvents = new()
        {
            new TestEvent("value1"),
        };
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            converter.ToStorageEvents(source, domainEvents));
        Assert.Contains("TestEvent", exception.Message, StringComparison.Ordinal);
    }
}