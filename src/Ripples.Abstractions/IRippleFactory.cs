namespace Mississippi.Ripples.Abstractions;

/// <summary>
///     Factory for creating <see cref="IRipple{T}" /> instances.
///     Different implementations exist for Server (direct grain) and Client (HTTP) scenarios.
/// </summary>
/// <typeparam name="T">The projection type.</typeparam>
public interface IRippleFactory<T>
    where T : class
{
    /// <summary>
    ///     Creates a new ripple instance for the specified projection type.
    /// </summary>
    /// <returns>A new ripple instance.</returns>
    IRipple<T> Create();
}