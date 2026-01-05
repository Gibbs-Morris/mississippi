using System;
using System.ComponentModel;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Abstractions.Attributes.Tests;

/// <summary>
///     Tests for <see cref="SnapshotStorageNameHelper" /> functionality.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Brooks Abstractions")]
[AllureSubSuite("Snapshot Storage Name Helper")]
public class SnapshotStorageNameHelperTests
{
    /// <summary>
    ///     Test fixture decorated with SnapshotStorageName attribute.
    /// </summary>
    [SnapshotStorageName("APP", "MODULE", "STATE", version: 1)]
    private sealed class DecoratedState
    {
    }

    /// <summary>
    ///     Test fixture without SnapshotStorageName attribute - used for type identity only.
    /// </summary>
    [Description("Test fixture without SnapshotStorageName attribute")]
    private sealed class UndecoratedState
    {
    }

    /// <summary>
    ///     Test fixture with versioned SnapshotStorageName attribute.
    /// </summary>
    [SnapshotStorageName("APP", "MODULE", "STATE", 2)]
    private sealed class VersionedState
    {
    }

    /// <summary>
    ///     GetStorageName should return the storage name from the attribute.
    /// </summary>
    [Fact]
    public void GetStorageNameReturnsAttributeValue()
    {
        string storageName = SnapshotStorageNameHelper.GetStorageName<DecoratedState>();
        Assert.Equal("APP.MODULE.STATE.V1", storageName);
    }

    /// <summary>
    ///     GetStorageName should return versioned storage name.
    /// </summary>
    [Fact]
    public void GetStorageNameReturnsVersionedName()
    {
        string storageName = SnapshotStorageNameHelper.GetStorageName<VersionedState>();
        Assert.Equal("APP.MODULE.STATE.V2", storageName);
    }

    /// <summary>
    ///     GetStorageName should throw when type lacks the attribute.
    /// </summary>
    [Fact]
    public void GetStorageNameThrowsWhenAttributeMissing()
    {
        Assert.Throws<InvalidOperationException>(() => SnapshotStorageNameHelper.GetStorageName<UndecoratedState>());
    }

    /// <summary>
    ///     GetStorageName with Type parameter should throw when type is null.
    /// </summary>
    [Fact]
    public void GetStorageNameWithTypeThrowsWhenNull()
    {
        Assert.Throws<ArgumentNullException>(() => SnapshotStorageNameHelper.GetStorageName(null!));
    }

    /// <summary>
    ///     TryGetStorageName should return false when attribute is missing.
    /// </summary>
    [Fact]
    public void TryGetStorageNameReturnsFalseWhenAttributeMissing()
    {
        bool result = SnapshotStorageNameHelper.TryGetStorageName<UndecoratedState>(out string? storageName);
        Assert.False(result);
        Assert.Null(storageName);
    }

    /// <summary>
    ///     TryGetStorageName should return true and the storage name when attribute exists.
    /// </summary>
    [Fact]
    public void TryGetStorageNameReturnsTrueWhenAttributeExists()
    {
        bool result = SnapshotStorageNameHelper.TryGetStorageName<DecoratedState>(out string? storageName);
        Assert.True(result);
        Assert.Equal("APP.MODULE.STATE.V1", storageName);
    }

    /// <summary>
    ///     TryGetStorageName with Type parameter should throw when type is null.
    /// </summary>
    [Fact]
    public void TryGetStorageNameWithTypeThrowsWhenNull()
    {
        Assert.Throws<ArgumentNullException>(() => SnapshotStorageNameHelper.TryGetStorageName(null!, out string? _));
    }
}