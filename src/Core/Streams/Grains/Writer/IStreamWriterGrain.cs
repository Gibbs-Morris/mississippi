using System.Collections.Immutable;

using Mississippi.Core.Streams.Grains.Reader;


namespace Mississippi.Core.Streams.Grains.Writer;

/// <summary>
///     Orleans grain contract that provides append (write) operations for an event stream.
/// </summary>
/// <remarks>
///     Implementations are keyed by the string returned from
///     <see cref="Mississippi.Core.Keys.StreamGrainKey" />, ensuring writes are scoped
///     to the correct event stream.
/// </remarks>
[Alias("Mississippi.Core.IStreamWriterGrain")]
public interface IStreamWriterGrain : IGrainWithStringKey
{
    [Alias("AppendEventsAsync")]
    Task<StreamPosition> AppendEventsAsync(
        ImmutableArray<MississippiEvent> events,
        StreamPosition? expectedHeadPosition = null,
        CancellationToken cancellationToken = default
    );
}