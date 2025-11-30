using Mississippi.Projections.Reducers;


namespace Mississippi.Projections.TempSamples;

/// <summary>
///     Marks the projection as inactive and stamps the deactivation timestamp.
/// </summary>
internal sealed class UserDeactivatedReducer : ReducerBase<UserProjection, UserDeactivated>
{
    /// <inheritdoc />
    protected override UserProjection Apply(
        UserProjection model,
        UserDeactivated domainEvent
    )
    {
        UserProjection current = UserProjectionGuards.EnsureRegistered(model, nameof(UserDeactivatedReducer));
        return current with
        {
            IsActive = false,
            DeactivatedAt = domainEvent.DeactivatedAt,
        };
    }
}