using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Orleans;
using Orleans.Concurrency;


namespace Mississippi.Core.Projection;

/// <summary>
///     Grain interface for tracking the cursor position of a projection.
///     Provides read-only access to the current cursor position of a projection.
/// </summary>
/// <typeparam name="TModel">The type of the projection model.</typeparam>
[Alias("Mississippi.Core.Projection.IProjectionCursorGrain")]
[SuppressMessage(
    "Major Code Smell",
    "S2326:Unused type parameters should be removed",
    Justification = "Generic parameter is required for Orleans grain type resolution at runtime.")]
public interface IProjectionCursorGrain<TModel> : IGrainWithStringKey
{
    /// <summary>
    ///     Gets the current cursor position of the projection.
    ///     The cursor position indicates the latest event sequence number that has been processed by the projection.
    /// </summary>
    /// <returns>The cursor position as a long value.</returns>
    [ReadOnly]
    [Alias("GetCursorPositionAsync")]
    Task<long> GetCursorPositionAsync();
}