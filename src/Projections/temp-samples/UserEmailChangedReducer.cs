using Mississippi.Projections.Reducers;


namespace Mississippi.Projections.TempSamples;

/// <summary>
///     Updates the email field on the projection whenever a <see cref="UserEmailChanged" /> event arrives.
/// </summary>
internal sealed class UserEmailChangedReducer : ReducerBase<UserProjection, UserEmailChanged>
{
    /// <inheritdoc />
    protected override UserProjection Apply(
        UserProjection model,
        UserEmailChanged domainEvent
    )
    {
        UserProjection current = UserProjectionGuards.EnsureRegistered(model, nameof(UserEmailChangedReducer));
        return current with
        {
            Email = domainEvent.Email,
        };
    }
}