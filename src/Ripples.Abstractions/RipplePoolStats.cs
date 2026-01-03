namespace Mississippi.Ripples.Abstractions;

/// <summary>
///     Statistics for a ripple pool's current state.
/// </summary>
/// <param name="HotCount">Number of active subscriptions (visible rows).</param>
/// <param name="WarmCount">Number of cached entries without subscription.</param>
/// <param name="TotalFetches">Total number of HTTP/grain requests made.</param>
/// <param name="CacheHits">Number of requests served from cache.</param>
public sealed record RipplePoolStats(int HotCount, int WarmCount, int TotalFetches, int CacheHits)
{
    /// <summary>
    ///     Gets an empty stats instance with all values at zero.
    /// </summary>
    public static RipplePoolStats Empty { get; } = new(0, 0, 0, 0);
}