using System;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.Testing.Utilities;


namespace Mississippi.PotetnalBugs.L0Tests;

/// <summary>
///     Validates null-handling behavior in <see cref="BrookAsyncReaderKey" /> parsing APIs.
/// </summary>
public sealed class BrookAsyncReaderKeyParseTests
{
    /// <summary>
    ///     Parse does not guard null input as missing argument. Instead, null is treated as malformed format
    ///     and callers receive a generic <see cref="ArgumentException" />.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "BrookAsyncReaderKey.Parse does not validate null input as a missing argument. " +
        "Passing null yields a format-style ArgumentException instead of ArgumentNullException.",
        FilePath = "src/EventSourcing.Brooks.Abstractions/BrookAsyncReaderKey.cs",
        LineNumbers = "84-88",
        Severity = "Low",
        Category = "NullReferenceRisk")]
    public void ParseNullThrowsFormatStyleArgumentException()
    {
        // Act
        ArgumentException ex = Assert.Throws<ArgumentException>(() => BrookAsyncReaderKey.Parse(null!));

        // Assert - null is reported as format error instead of missing argument
        Assert.Contains("missing first separator", ex.Message, StringComparison.Ordinal);
    }
}
