using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Aggregates;
using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.Testing.Utilities;


namespace Mississippi.PotetnalBugs.L0Tests;

/// <summary>
///     Validates potential bugs in <see cref="ISnapshotTypeRegistry" /> behavior.
/// </summary>
public sealed class SnapshotTypeRegistryTests
{
    /// <summary>
    ///     Registering the same CLR snapshot type under two different names creates an asymmetric registry state:
    ///     both names resolve to the type, but type-to-name resolution still returns only the first name.
    ///     This violates the "first registration wins" intent in the implementation comment.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "SnapshotTypeRegistry.Register allows adding a second snapshot name for the same CLR type. " +
        "nameToType accepts the alias, but typeToName keeps only the first mapping, creating " +
        "an asymmetric registry state and violating the 'first registration wins' intent.",
        FilePath = "src/EventSourcing.Aggregates/SnapshotTypeRegistry.cs",
        LineNumbers = "45-48",
        Severity = "Medium",
        Category = "DeveloperExperience")]
    public void RegisteringSameTypeWithDifferentNamesCreatesAsymmetricMappings()
    {
        // Arrange
        ServiceCollection services = new();
        _ = services.AddAggregateSupport();
        using ServiceProvider provider = services.BuildServiceProvider();
        ISnapshotTypeRegistry sut = provider.GetRequiredService<ISnapshotTypeRegistry>();

        const string firstName = "APP.AGG.SNAPSHOT_ONE.V1";
        const string secondName = "APP.AGG.SNAPSHOT_ALIAS.V1";

        // Act
        sut.Register(firstName, typeof(TestSnapshot));
        sut.Register(secondName, typeof(TestSnapshot));

        // Assert - both names resolve to the same type, but reverse lookup only keeps the first name
        Assert.Equal(2, sut.RegisteredTypes.Count);
        Assert.Equal(typeof(TestSnapshot), sut.ResolveType(firstName));
        Assert.Equal(typeof(TestSnapshot), sut.ResolveType(secondName));
        Assert.Equal(firstName, sut.ResolveName(typeof(TestSnapshot)));
    }

    /// <summary>
    ///     ScanAssembly reports every attributed type as registered even when duplicate storage names
    ///     are ignored by Register. This overstates how many types were actually added to the registry.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "SnapshotTypeRegistry.ScanAssembly increments its return count for every attributed type " +
        "before considering duplicate storage names. Duplicate names are ignored by Register, " +
        "so ScanAssembly can report more registrations than actually exist in RegisteredTypes.",
        FilePath = "src/EventSourcing.Aggregates/SnapshotTypeRegistry.cs",
        LineNumbers = "71-86",
        Severity = "Low",
        Category = "LogicError")]
    public void ScanAssemblyOverreportsWhenDuplicateStorageNamesExist()
    {
        // Arrange
        ServiceCollection services = new();
        _ = services.AddAggregateSupport();
        using ServiceProvider provider = services.BuildServiceProvider();
        ISnapshotTypeRegistry sut = provider.GetRequiredService<ISnapshotTypeRegistry>();

        // Act
        int scannedCount = sut.ScanAssembly(typeof(DuplicateSnapshotNameOne).Assembly);

        // Assert - scan count includes duplicates while registry map keeps one name entry
        Assert.Equal(2, scannedCount);
        _ = Assert.Single(sut.RegisteredTypes);
    }

    private sealed record TestSnapshot(
        string Value
    );

    [SnapshotStorageName("POTBUG", "SNAP", "DUPLICATE", 1)]
    private sealed record DuplicateSnapshotNameOne(
        string Value
    );

    [SnapshotStorageName("POTBUG", "SNAP", "DUPLICATE", 1)]
    private sealed record DuplicateSnapshotNameTwo(
        string Value
    );
}
