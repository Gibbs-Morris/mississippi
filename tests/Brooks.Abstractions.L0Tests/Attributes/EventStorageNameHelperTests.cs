using System;

using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.Brooks.Abstractions.L0Tests.Attributes;

/// <summary>
///     Tests for <see cref="EventStorageNameHelper" /> functionality.
/// </summary>
public class EventStorageNameHelperTests
{
    /// <summary>
    ///     First generic argument used for closed generic storage-name tests.
    /// </summary>
    [Alias("Mississippi.Brooks.Abstractions.L0Tests.Attributes.FirstGenericArgument")]
    internal sealed record FirstGenericArgument;

    /// <summary>
    ///     Second generic argument used for closed generic storage-name tests.
    /// </summary>
    [Alias("Mississippi.Brooks.Abstractions.L0Tests.Attributes.SecondGenericArgument")]
    internal sealed record SecondGenericArgument;

    /// <summary>
    ///     Test fixture decorated with EventStorageName attribute.
    /// </summary>
    [EventStorageName("APP", "MODULE", "EVENT")]
    private sealed class DecoratedEvent
    {
    }

    [EventStorageName("APP", "MODULE", "GENERICEVENT")]
    private sealed record GenericEvent<T>(T Value)
        where T : class;

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
    ///     GetStorageName should generate distinct names for different closed generic types.
    /// </summary>
    [Fact]
    public void GetStorageNameReturnsDistinctNamesForClosedGenericTypes()
    {
        string firstStorageName = EventStorageNameHelper.GetStorageName<GenericEvent<FirstGenericArgument>>();
        string secondStorageName = EventStorageNameHelper.GetStorageName<GenericEvent<SecondGenericArgument>>();
        Assert.NotEqual(firstStorageName, secondStorageName);
        Assert.StartsWith("APP.MODULE.GENERICEVENTG", firstStorageName, StringComparison.Ordinal);
        Assert.StartsWith("APP.MODULE.GENERICEVENTG", secondStorageName, StringComparison.Ordinal);
        Assert.EndsWith(".V1", firstStorageName, StringComparison.Ordinal);
        Assert.EndsWith(".V1", secondStorageName, StringComparison.Ordinal);
    }

    /// <summary>
    ///     GetStorageName should be stable for the same closed generic type.
    /// </summary>
    [Fact]
    public void GetStorageNameReturnsStableNameForClosedGenericType()
    {
        string firstStorageName = EventStorageNameHelper.GetStorageName<GenericEvent<FirstGenericArgument>>();
        Type closedGenericType = typeof(GenericEvent<>).MakeGenericType(typeof(FirstGenericArgument));
        string secondStorageName = EventStorageNameHelper.GetStorageName(closedGenericType);
        Assert.Equal(firstStorageName, secondStorageName);
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
    ///     TryGetStorageName should return generated storage name for a closed generic type.
    /// </summary>
    [Fact]
    public void TryGetStorageNameReturnsGeneratedNameForClosedGenericType()
    {
        Type closedGenericType = typeof(GenericEvent<>).MakeGenericType(typeof(FirstGenericArgument));
        bool result = EventStorageNameHelper.TryGetStorageName(closedGenericType, out string? storageName);
        Assert.True(result);
        Assert.NotNull(storageName);
        Assert.StartsWith("APP.MODULE.GENERICEVENTG", storageName, StringComparison.Ordinal);
        Assert.EndsWith(".V1", storageName, StringComparison.Ordinal);
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