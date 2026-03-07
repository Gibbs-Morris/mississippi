using Mississippi.Spring.Domain.Aggregates.AuthProof.Events;
using Mississippi.Tributary.Abstractions;


namespace Mississippi.Spring.Domain.Projections.AuthProof.Reducers;

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