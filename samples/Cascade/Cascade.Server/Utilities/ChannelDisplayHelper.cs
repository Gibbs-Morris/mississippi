using System;
using System.Diagnostics.CodeAnalysis;


namespace Cascade.Server.Utilities;

/// <summary>
///     Provides utility methods for channel display formatting.
/// </summary>
internal static class ChannelDisplayHelper
{
    /// <summary>
    ///     Extracts a readable display name from a channel ID.
    /// </summary>
    /// <param name="channelId">The channel ID in format "channel-NAME-TIMESTAMP".</param>
    /// <returns>The extracted channel name in lowercase, or the original ID if format is unrecognized.</returns>
    [SuppressMessage(
        "Globalization",
        "CA1308:Normalize strings to uppercase",
        Justification = "ToLowerInvariant is used intentionally for UI display aesthetics.")]
    public static string GetDisplayName(
        string channelId
    )
    {
        // Extract a readable name from the channel ID
        // Format is typically: channel-NAME-TIMESTAMP
        if (channelId.StartsWith("channel-", StringComparison.OrdinalIgnoreCase))
        {
            string[] parts = channelId[8..].Split('-');
            if (parts.Length >= 1)
            {
                return parts[0].ToLowerInvariant();
            }
        }

        return channelId;
    }
}