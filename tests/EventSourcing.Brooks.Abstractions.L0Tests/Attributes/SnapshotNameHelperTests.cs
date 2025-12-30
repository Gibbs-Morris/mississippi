using System;
using System.ComponentModel;

using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Abstractions.Attributes.Tests;

/// <summary>
///     Tests for <see cref="SnapshotNameHelper" /> functionality.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Brooks Abstractions")]
[AllureSubSuite("Snapshot Name Helper")]
public class SnapshotNameHelperTests
{
    /// <summary>
    ///     Test fixture decorated with SnapshotName attribute.
    /// </summary>
    [SnapshotName("APP", "MODULE", "STATE")]
    private sealed class DecoratedState
    {
    }

    /// <summary>
    ///     Test fixture without SnapshotName attribute - used for type identity only.
    /// </summary>
    [Description("Test fixture without SnapshotName attribute")]
    private sealed class UndecoratedState
    {
    }

    /// <summary>
    ///     Test fixture with versioned SnapshotName attribute.
    /// </summary>
    [SnapshotName("APP", "MODULE", "STATE", 2)]
    private sealed class VersionedState
    {
    }

    /// <summary>
    ///     GetSnapshotName should return the snapshot name from the attribute.
    /// </summary>
    [Fact]
    public void GetSnapshotNameReturnsAttributeValue()
    {
        string snapshotName = SnapshotNameHelper.GetSnapshotName<DecoratedState>();
        Assert.Equal("APP.MODULE.STATE.V1", snapshotName);
    }

    /// <summary>
    ///     GetSnapshotName should return versioned snapshot name.
    /// </summary>
    [Fact]
    public void GetSnapshotNameReturnsVersionedName()
    {
        string snapshotName = SnapshotNameHelper.GetSnapshotName<VersionedState>();
        Assert.Equal("APP.MODULE.STATE.V2", snapshotName);
    }

    /// <summary>
    ///     GetSnapshotName should throw when type lacks the attribute.
    /// </summary>
    [Fact]
    public void GetSnapshotNameThrowsWhenAttributeMissing()
    {
        Assert.Throws<InvalidOperationException>(() => SnapshotNameHelper.GetSnapshotName<UndecoratedState>());
    }

    /// <summary>
    ///     GetSnapshotName with Type parameter should throw when type is null.
    /// </summary>
    [Fact]
    public void GetSnapshotNameWithTypeThrowsWhenNull()
    {
        Assert.Throws<ArgumentNullException>(() => SnapshotNameHelper.GetSnapshotName(null!));
    }

    /// <summary>
    ///     TryGetSnapshotName should return false when attribute is missing.
    /// </summary>
    [Fact]
    public void TryGetSnapshotNameReturnsFalseWhenAttributeMissing()
    {
        bool result = SnapshotNameHelper.TryGetSnapshotName<UndecoratedState>(out string? snapshotName);
        Assert.False(result);
        Assert.Null(snapshotName);
    }

    /// <summary>
    ///     TryGetSnapshotName should return true and the snapshot name when attribute exists.
    /// </summary>
    [Fact]
    public void TryGetSnapshotNameReturnsTrueWhenAttributeExists()
    {
        bool result = SnapshotNameHelper.TryGetSnapshotName<DecoratedState>(out string? snapshotName);
        Assert.True(result);
        Assert.Equal("APP.MODULE.STATE.V1", snapshotName);
    }

    /// <summary>
    ///     TryGetSnapshotName with Type parameter should throw when type is null.
    /// </summary>
    [Fact]
    public void TryGetSnapshotNameWithTypeThrowsWhenNull()
    {
        Assert.Throws<ArgumentNullException>(() => SnapshotNameHelper.TryGetSnapshotName(null!, out string? _));
    }
}