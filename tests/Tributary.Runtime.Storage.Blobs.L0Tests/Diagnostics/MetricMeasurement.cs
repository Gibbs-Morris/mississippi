using System.Collections.Generic;


namespace MississippiTests.Tributary.Runtime.Storage.Blobs.L0Tests.Diagnostics;

/// <summary>
///     Captures one metric measurement for assertions.
/// </summary>
/// <param name="InstrumentName">The instrument name.</param>
/// <param name="LongValue">The integer measurement.</param>
/// <param name="DoubleValue">The floating-point measurement.</param>
/// <param name="Tags">The measurement tags.</param>
internal sealed record MetricMeasurement(
    string InstrumentName,
    long LongValue,
    double DoubleValue,
    IReadOnlyDictionary<string, object?> Tags
);