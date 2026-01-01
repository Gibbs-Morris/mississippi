using Crescent.NewModel.Chat;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.UxProjections;
using Mississippi.EventSourcing.UxProjections.Abstractions;

using Orleans.Runtime;


namespace Crescent.NewModel.ChatSummary;

/// <summary>
///     UX projection grain providing cached, read-optimized access to
///     <see cref="ChatSummaryProjection" /> state.
/// </summary>
/// <remarks>
///     <para>
///         This grain demonstrates the "multiple projections per brook" pattern.
///         It consumes the same <see cref="ChatBrook" /> event stream as the
///         <see cref="ChatAggregateGrain" />, but produces a read-optimized
///         projection for UX display purposes.
///     </para>
///     <para>
///         The grain is a stateless worker that caches the last returned projection
///         in memory. On each request, it checks the cursor position and only fetches
///         a new snapshot if the brook has advanced since the last read.
///     </para>
/// </remarks>
internal sealed class ChatSummaryProjectionGrain : UxProjectionGrainBase<ChatSummaryProjection, ChatBrook>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ChatSummaryProjectionGrain" /> class.
    /// </summary>
    /// <param name="grainContext">The Orleans grain context.</param>
    /// <param name="uxProjectionGrainFactory">Factory for resolving UX projection grains and cursors.</param>
    /// <param name="logger">Logger instance.</param>
    public ChatSummaryProjectionGrain(
        IGrainContext grainContext,
        IUxProjectionGrainFactory uxProjectionGrainFactory,
        ILogger<ChatSummaryProjectionGrain> logger
    )
        : base(grainContext, uxProjectionGrainFactory, logger)
    {
    }
}
