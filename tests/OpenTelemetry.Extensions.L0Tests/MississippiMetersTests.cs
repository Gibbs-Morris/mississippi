using System.Collections.Generic;
using System.Linq;

using Allure.Xunit.Attributes;


namespace Mississippi.OpenTelemetry.Extensions.L0Tests;

/// <summary>
///     Tests for <see cref="MississippiMeters" /> constants.
/// </summary>
[AllureParentSuite("Mississippi.OpenTelemetry.Extensions")]
[AllureSuite("Core")]
[AllureSubSuite("MississippiMeters")]
public sealed class MississippiMetersTests
{
    /// <summary>
    ///     Aggregates meter name should be "Mississippi.EventSourcing.Aggregates".
    /// </summary>
    [Fact]
    [AllureFeature("Meter Names")]
    public void AggregatesHasExpectedValue()
    {
        // Assert
        Assert.Equal("Mississippi.EventSourcing.Aggregates", MississippiMeters.Aggregates);
    }

    /// <summary>
    ///     All property should return all meter names.
    /// </summary>
    [Fact]
    [AllureFeature("Meter Names")]
    public void AllReturnsAllMeterNames()
    {
        // Arrange
        IReadOnlyList<string> all = MississippiMeters.All;

        // Assert
        Assert.Equal(8, all.Count);
        Assert.Contains(MississippiMeters.Aqueduct, all);
        Assert.Contains(MississippiMeters.Aggregates, all);
        Assert.Contains(MississippiMeters.Brooks, all);
        Assert.Contains(MississippiMeters.Inlet, all);
        Assert.Contains(MississippiMeters.Snapshots, all);
        Assert.Contains(MississippiMeters.StorageLocking, all);
        Assert.Contains(MississippiMeters.StorageSnapshots, all);
        Assert.Contains(MississippiMeters.UxProjections, all);
    }

    /// <summary>
    ///     Aqueduct meter name should be "Mississippi.Aqueduct".
    /// </summary>
    [Fact]
    [AllureFeature("Meter Names")]
    public void AqueductHasExpectedValue()
    {
        // Assert
        Assert.Equal("Mississippi.Aqueduct", MississippiMeters.Aqueduct);
    }

    /// <summary>
    ///     Brooks meter name should be "Mississippi.EventSourcing.Brooks".
    /// </summary>
    [Fact]
    [AllureFeature("Meter Names")]
    public void BrooksHasExpectedValue()
    {
        // Assert
        Assert.Equal("Mississippi.EventSourcing.Brooks", MississippiMeters.Brooks);
    }

    /// <summary>
    ///     Inlet meter name should be "Mississippi.Inlet".
    /// </summary>
    [Fact]
    [AllureFeature("Meter Names")]
    public void InletHasExpectedValue()
    {
        // Assert
        Assert.Equal("Mississippi.Inlet", MississippiMeters.Inlet);
    }

    /// <summary>
    ///     Snapshots meter name should be "Mississippi.EventSourcing.Snapshots".
    /// </summary>
    [Fact]
    [AllureFeature("Meter Names")]
    public void SnapshotsHasExpectedValue()
    {
        // Assert
        Assert.Equal("Mississippi.EventSourcing.Snapshots", MississippiMeters.Snapshots);
    }

    /// <summary>
    ///     StorageLocking meter name should be "Mississippi.Storage.Locking".
    /// </summary>
    [Fact]
    [AllureFeature("Meter Names")]
    public void StorageLockingHasExpectedValue()
    {
        // Assert
        Assert.Equal("Mississippi.Storage.Locking", MississippiMeters.StorageLocking);
    }

    /// <summary>
    ///     StorageSnapshots meter name should be "Mississippi.Storage.Snapshots".
    /// </summary>
    [Fact]
    [AllureFeature("Meter Names")]
    public void StorageSnapshotsHasExpectedValue()
    {
        // Assert
        Assert.Equal("Mississippi.Storage.Snapshots", MississippiMeters.StorageSnapshots);
    }

    /// <summary>
    ///     UxProjections meter name should be "Mississippi.EventSourcing.UxProjections".
    /// </summary>
    [Fact]
    [AllureFeature("Meter Names")]
    public void UxProjectionsHasExpectedValue()
    {
        // Assert
        Assert.Equal("Mississippi.EventSourcing.UxProjections", MississippiMeters.UxProjections);
    }
}
