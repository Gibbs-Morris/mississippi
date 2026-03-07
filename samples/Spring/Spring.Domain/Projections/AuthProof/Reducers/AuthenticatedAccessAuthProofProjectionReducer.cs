using Mississippi.Tributary.Abstractions;

using Spring.Domain.Aggregates.AuthProof.Events;


namespace Spring.Domain.Projections.AuthProof.Reducers;

/// <summary>
///     Reduces authenticated access events into the auth-proof projection.
/// </summary>
internal sealed class AuthenticatedAccessAuthProofProjectionReducer
    : EventReducerBase<AuthenticatedAccessRecorded, AuthProofProjection>
{
    /// <inheritdoc />
    protected override AuthProofProjection ReduceCore(
        AuthProofProjection state,
        AuthenticatedAccessRecorded @event
    ) =>
        (state ?? new()) with
        {
            AuthenticatedAccessCount = (state?.AuthenticatedAccessCount ?? 0) + 1,
        };
}