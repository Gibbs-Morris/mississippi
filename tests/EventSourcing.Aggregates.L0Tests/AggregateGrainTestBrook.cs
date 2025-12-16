using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Abstractions.Attributes;

namespace Mississippi.EventSourcing.Aggregates.Tests;

/// <summary>
///     Test brook definition for AggregateGrain tests.
/// </summary>
[BrookName("TEST", "AGGREGATES", "TESTBROOK")]
internal sealed class AggregateGrainTestBrook : IBrookDefinition
{
    /// <inheritdoc />
    public static string BrookName => "TEST.AGGREGATES.TESTBROOK";
}
