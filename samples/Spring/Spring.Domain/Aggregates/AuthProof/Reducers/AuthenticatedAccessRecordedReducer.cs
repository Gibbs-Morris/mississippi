using Mississippi.Spring.Domain.Aggregates.AuthProof.Events;
using Mississippi.Tributary.Abstractions;


namespace Mississippi.Spring.Domain.Aggregates.AuthProof.Reducers;

/// <summary>
///     Reducer for <see cref="AuthenticatedAccessRecorded" /> events.
/// </summary>
internal sealed class AuthenticatedAccessRecordedReducer
    : EventReducerBase<AuthenticatedAccessRecorded, AuthProofAggregate>
{
    /// <inheritdoc />
    protected override AuthProofAggregate ReduceCore(
        AuthProofAggregate state,
        AuthenticatedAccessRecorded @event
    ) =>
        (state ?? new()) with
        {
            AuthenticatedAccessCount = (state?.AuthenticatedAccessCount ?? 0) + 1,
        };
}