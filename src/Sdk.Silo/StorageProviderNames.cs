namespace Mississippi.Sdk.Silo;

/// <summary>
///     Storage provider names for Orleans grain storage.
/// </summary>
public sealed class StorageProviderNames
{
    /// <summary>
    ///     Gets or sets the PubSub grain storage name.
    /// </summary>
    public string PubSub { get; set; } = "PubSubStore";

    /// <summary>
    ///     Gets or sets the event log grain storage name.
    /// </summary>
    public string EventLog { get; set; } = "event-log";

    /// <summary>
    ///     Gets or sets the snapshot grain storage name.
    /// </summary>
    public string Snapshots { get; set; } = "snapshots";

    /// <summary>
    ///     Gets or sets the projections grain storage name.
    /// </summary>
    public string Projections { get; set; } = "projections";
}
