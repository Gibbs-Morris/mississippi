using System.Collections.Generic;
using System.Linq;


namespace Crescent.ConsoleApp;

/// <summary>
///     Represents persisted identifiers and known cursors used by the console scenarios to resume work.
/// </summary>
internal sealed class RunState
{
    /// <summary>
    ///     Gets or sets the last confirmed cursor position of the primary stream, if known.
    /// </summary>
    public long? PrimaryCursor { get; set; }

    /// <summary>
    ///     Gets or sets the primary stream identifier for the main scenario.
    /// </summary>
    public string? PrimaryId { get; set; }

    /// <summary>
    ///     Gets or sets the primary stream type for the main scenario.
    /// </summary>
    public string? PrimaryType { get; set; }

    /// <summary>
    ///     Gets or sets the collection of known stream cursors captured during multi-stream scenarios.
    /// </summary>
    public List<StreamState> Streams { get; set; } = new();

    /// <summary>
    ///     Adds a new stream cursor entry or updates the existing one.
    /// </summary>
    /// <param name="type">The stream type.</param>
    /// <param name="id">The stream identifier.</param>
    /// <param name="cursor">The latest known cursor position.</param>
    public void UpsertStream(
        string type,
        string id,
        long cursor
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
                    Cursor = cursor,
                });
        }
        else
        {
            existing.Cursor = cursor;
        }
    }
}