using Mississippi.Projections.Reducers;


namespace Mississippi.Projections.TempSamples;

/// <summary>
///     Creates the initial <see cref="UserProjection" /> when a <see cref="UserRegistered" /> event arrives.
/// </summary>
internal sealed class UserRegisteredReducer : ReducerBase<UserProjection, UserRegistered>
{
    /// <inheritdoc />
    protected override UserProjection Apply(
        UserProjection model,
        UserRegistered domainEvent
    )
    {
        UserProjection current = UserProjection.Ensure(model);
        return current with
        {
            UserId = domainEvent.UserId,
            Email = domainEvent.Email,
            DisplayName = domainEvent.DisplayName,
            IsActive = true,
            RegisteredAt = domainEvent.RegisteredAt,
            DeactivatedAt = null,
        };
    }
}