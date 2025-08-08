namespace Mississippi.CrescentConsoleApp;

/// <summary>
///     Represents persisted identifiers and known heads used by the console scenarios to resume work.
/// </summary>
internal sealed class RunState
{
    /// <summary>
    ///     Gets or sets the primary stream type for the main scenario.
    /// </summary>
    public string? PrimaryType { get; set; }

    /// <summary>
    ///     Gets or sets the primary stream identifier for the main scenario.
    /// </summary>
    public string? PrimaryId { get; set; }

    /// <summary>
    ///     Gets or sets the last confirmed head position of the primary stream, if known.
    /// </summary>
    public long? PrimaryHead { get; set; }

    /// <summary>
    ///     Gets or sets the collection of known stream heads captured during multi-stream scenarios.
    /// </summary>
    public List<StreamState> Streams { get; set; } = new();

    /// <summary>
    ///     Adds a new stream head entry or updates the existing one.
    /// </summary>
    /// <param name="type">The stream type.</param>
    /// <param name="id">The stream identifier.</param>
    /// <param name="head">The latest known head position.</param>
    public void UpsertStream(
        string type,
        string id,
        long head
    )
    {
        StreamState? existing = Streams.FirstOrDefault(s => (s.Type == type) && (s.Id == id));
        if (existing is null)
        {
            Streams.Add(
                new()
                {
                    Type = type,
                    Id = id,
                    Head = head,
                });
        }
        else
        {
            existing.Head = head;
        }
    }
}