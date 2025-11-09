using System;


namespace Mississippi.Core.Projection;

/// <summary>
///     Interface for finding appropriate snapshot versions based on position and step values.
///     Provides utility methods for snapshot management in projections.
/// </summary>
public interface ISnapshotVersionFinder
{
    /// <summary>
    ///     Finds the appropriate snapshot position based on a given value and step size.
    ///     Calculates the nearest valid snapshot position that is less than or equal to the specified value.
    /// </summary>
    /// <param name="value">The target value to find a snapshot position for.</param>
    /// <param name="step">The step size between valid snapshot positions. Must be greater than 0.</param>
    /// <returns>The calculated snapshot position, or 0 if step is invalid.</returns>
    public static long FindSnapshot(
        long value,
        long step
    )
    {
        if (step <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(step), "Step must be greater than 0");
        }

        return (value / step) * step;
    }
}