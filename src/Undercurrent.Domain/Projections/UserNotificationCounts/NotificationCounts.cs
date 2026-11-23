using System.Collections.Immutable;
using System.Linq;

using Mississippi.Undercurrent.Abstractions.Projections.UserNotificationCounts;


namespace Mississippi.Undercurrent.Domain.Projections.UserNotificationCounts;

/// <summary>Keeps cached counters consistent with the retained ID/read-status index.</summary>
internal static class NotificationCounts
{
    /// <summary>Creates a new projection with counters derived from the complete updated index.</summary>
    /// <param name="state">The preceding projection.</param>
    /// <param name="readStates">The updated retained notification index.</param>
    /// <returns>The consistent projection.</returns>
    public static UserNotificationCountsProjection Update(
        UserNotificationCountsProjection state,
        ImmutableDictionary<string, bool> readStates
    ) =>
        state with
        {
            ReadStates = readStates,
            TotalCount = readStates.Count,
            UnreadCount = readStates.Count(pair => !pair.Value),
        };
}