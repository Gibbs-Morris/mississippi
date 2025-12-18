using System.Diagnostics;


namespace Mississippi.Observability;

/// <summary>
///     Provides the ActivitySource for serialization operations.
/// </summary>
public static class SerializationTelemetry
{
    /// <summary>
    ///     ActivitySource for tracing serialization operations.
    /// </summary>
    public static readonly ActivitySource Source = new(
        MississippiActivitySources.Serialization,
        MississippiActivitySources.Version);
}
