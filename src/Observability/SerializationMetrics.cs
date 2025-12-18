using System.Diagnostics.Metrics;


namespace Mississippi.Observability;

/// <summary>
///     Provides metrics instrumentation for serialization operations.
/// </summary>
public static class SerializationMetrics
{
    /// <summary>
    ///     Meter for serialization operation metrics.
    /// </summary>
    public static readonly Meter Meter = new(
        MississippiMeters.Serialization,
        MississippiMeters.Version);

    /// <summary>
    ///     Counter for serialization operations.
    /// </summary>
    public static readonly Counter<long> SerializationCount = Meter.CreateCounter<long>(
        "mississippi.serialization.serialize_total",
        unit: "{operation}",
        description: "Total number of serialization operations");

    /// <summary>
    ///     Counter for deserialization operations.
    /// </summary>
    public static readonly Counter<long> DeserializationCount = Meter.CreateCounter<long>(
        "mississippi.serialization.deserialize_total",
        unit: "{operation}",
        description: "Total number of deserialization operations");
}
