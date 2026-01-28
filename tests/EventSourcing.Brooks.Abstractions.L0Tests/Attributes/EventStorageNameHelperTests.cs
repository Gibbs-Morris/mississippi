using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Brooks.Abstractions.L0Tests.Attributes;

/// <summary>
///     Tests for <see cref="EventStorageNameHelper" /> functionality.
/// </summary>
public class EventStorageNameHelperTests
{
    /// <summary>
    ///     Test fixture decorated with EventStorageName attribute.
    /// </summary>
    [EventStorageName("APP", "MODULE", "EVENT")]
    private sealed class DecoratedEvent
    {
    }

    /// <summary>
    ///     Test fixture without EventStorageName attribute.
    /// </summary>
    private sealed class UndecoratedEvent
    {
    }

    /// <summary>
    ///     Test fixture with versioned EventStorageName attribute.
    /// </summary>
    [EventStorageName("APP", "MODULE", "EVENT", 2)]
    private sealed class VersionedEvent
    {
    }

    /// <summary>
    ///     GetStorageName should return the storage name from the attribute.
    /// </summary>
    [Fact]
    public void GetStorageNameReturnsAttributeValue()
    {
        string storageName = EventStorageNameHelper.GetStorageName<DecoratedEvent>();
        Assert.Equal("APP.MODULE.EVENT.V1", storageName);
    }

    /// <summary>
    ///     GetStorageName should return versioned storage name.
    /// </summary>
    [Fact]
    public void GetStorageNameReturnsVersionedName()
    {
        string storageName = EventStorageNameHelper.GetStorageName<VersionedEvent>();
        Assert.Equal("APP.MODULE.EVENT.V2", storageName);
    }

    /// <summary>
    ///     GetStorageName should throw when type lacks the attribute.
    /// </summary>
    [Fact]
    public void GetStorageNameThrowsWhenAttributeMissing()
    {
        Assert.Throws<InvalidOperationException>(() => EventStorageNameHelper.GetStorageName<UndecoratedEvent>());
    }

    /// <summary>
    ///     GetStorageName with Type parameter should throw when type is null.
    /// </summary>
    [Fact]
    public void GetStorageNameWithTypeThrowsWhenNull()
    {
        Assert.Throws<ArgumentNullException>(() => EventStorageNameHelper.GetStorageName(null!));
    }

    /// <summary>
    ///     TryGetStorageName should return false when attribute is missing.
    /// </summary>
    [Fact]
    public void TryGetStorageNameReturnsFalseWhenAttributeMissing()
    {
        bool result = EventStorageNameHelper.TryGetStorageName<UndecoratedEvent>(out string? storageName);
        Assert.False(result);
        Assert.Null(storageName);
    }

    /// <summary>
    ///     TryGetStorageName should return true and the storage name when attribute exists.
    /// </summary>
    [Fact]
    public void TryGetStorageNameReturnsTrueWhenAttributeExists()
    {
        bool result = EventStorageNameHelper.TryGetStorageName<DecoratedEvent>(out string? storageName);
        Assert.True(result);
        Assert.Equal("APP.MODULE.EVENT.V1", storageName);
    }

    /// <summary>
    ///     TryGetStorageName with Type parameter should throw when type is null.
    /// </summary>
    [Fact]
    public void TryGetStorageNameWithTypeThrowsWhenNull()
    {
        Assert.Throws<ArgumentNullException>(() => EventStorageNameHelper.TryGetStorageName(null!, out string? _));
    }
}