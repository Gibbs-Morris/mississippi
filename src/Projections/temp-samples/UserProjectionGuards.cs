using System;


namespace Mississippi.Projections.TempSamples;

/// <summary>
///     Helper methods to enforce invariant checks inside the sample reducers.
/// </summary>
internal static class UserProjectionGuards
{
    /// <summary>
    ///     Ensures that the projection has already been initialized via a <see cref="UserRegistered" /> event.
    /// </summary>
    /// <param name="projection">The projection to validate.</param>
    /// <param name="reducerName">The reducer requesting the guard.</param>
    /// <returns>The original projection when validation succeeds.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the projection has not been initialized.</exception>
    public static UserProjection EnsureRegistered(
        UserProjection? projection,
        string reducerName
    )
    {
        UserProjection current = UserProjection.Ensure(projection);
        if (current.UserId == Guid.Empty)
        {
            throw new InvalidOperationException(
                $"Reducer '{reducerName}' requires the user to be registered before applying the event.");
        }

        return current;
    }
}