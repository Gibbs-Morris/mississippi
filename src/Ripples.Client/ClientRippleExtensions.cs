using Mississippi.Ripples.Abstractions;


namespace Mississippi.Ripples.Client;

/// <summary>
///     Internal extension for setting prefetched data on a ripple.
/// </summary>
internal static class ClientRippleExtensions
{
    /// <summary>
    ///     Sets prefetched data directly on the ripple without a full subscription.
    /// </summary>
    /// <typeparam name="TProjection">The projection type.</typeparam>
    /// <param name="ripple">The ripple instance.</param>
    /// <param name="projection">The prefetched projection data.</param>
    public static void SetPrefetchedData<TProjection>(
        this IRipple<TProjection> ripple,
        TProjection projection
    )
        where TProjection : class
    {
        // For now this is a no-op; in a real implementation we'd need internal access
        // or an interface method to inject prefetched data
        _ = ripple;
        _ = projection;
    }
}