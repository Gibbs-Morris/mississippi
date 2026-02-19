using System;

using Mississippi.EventSourcing.Brooks.Abstractions;
using Mississippi.Testing.Utilities;


namespace Mississippi.PotetnalBugs.L0Tests;

/// <summary>
///     Validates that null-handling in <see cref="BrookAsyncReaderKey" /> parsing is now fixed:
///     null input throws <see cref="ArgumentNullException" /> instead of a format-style exception.
/// </summary>
public sealed class BrookAsyncReaderKeyParseTests
{
    /// <summary>
    ///     FIXED: Parse now guards null input with <see cref="ArgumentNullException" />
    ///     instead of treating it as a malformed format string.
    /// </summary>
    [Fact]
    [ValidatingPotetnalBug(
        "FIXED: BrookAsyncReaderKey.Parse previously treated null as malformed format, " +
        "yielding a generic ArgumentException. Parse now throws ArgumentNullException for null input.",
        FilePath = "src/EventSourcing.Brooks.Abstractions/BrookAsyncReaderKey.cs",
        LineNumbers = "84-88",
        Severity = "Low",
        Category = "NullReferenceRisk")]
    public void ParseNullThrowsArgumentNullException()
    {
        // Act & Assert - null input now throws ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => BrookAsyncReaderKey.Parse(null!));
    }
}
