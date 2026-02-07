using System;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.Testing.Utilities;


namespace Mississippi.PotetnalBugs.L0Tests;

/// <summary>
///     Validates that <c>default(BrookPosition)</c> bypasses constructor sentinel initialization.
/// </summary>
/// <remarks>
///     This bug cannot be safely fixed by normalizing the Value getter because position 0
///     is a valid first event position. Normalizing 0 → -1 would make legitimate position 0
///     indistinguishable from NotSet. A proper fix would require changing the storage
///     representation (e.g., nullable backing field or boolean flag), which is a breaking
///     serialization change.
/// </remarks>
public sealed class BrookPositionDefaultValueTests
{
    /// <summary>
    ///     The parameterless constructor sets the NotSet sentinel value (-1), but
    ///     <c>default(BrookPosition)</c> bypasses constructors and yields Value = 0.
    ///     This makes default values look like a valid first event position.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "BrookPosition defines a NotSet sentinel of -1 in its parameterless constructor, " +
        "but default(BrookPosition) bypasses constructors and produces Value = 0. " +
        "As a result, default values are treated as set positions instead of NotSet. " +
        "Cannot safely fix: normalizing 0 to -1 breaks valid position 0.",
        FilePath = "src/EventSourcing.Brooks.Abstractions/BrookPosition.cs",
        LineNumbers = "37,45,51",
        Severity = "Medium",
        Category = "MissingValidation")]
    public void DefaultBrookPositionIsTreatedAsSetInsteadOfNotSet()
    {
        // Arrange - explicit construction uses the sentinel constructor
        BrookPosition constructed = new();

        // Act - default bypasses struct constructor logic
        BrookPosition fromDefault = default;

        // Assert - constructor and default produce different invariants
        Assert.True(constructed.NotSet);
        Assert.Equal(-1L, constructed.Value);
        Assert.False(fromDefault.NotSet);
        Assert.Equal(0L, fromDefault.Value);
    }
}
