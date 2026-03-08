using Mississippi.Tributary.Abstractions;

using MississippiSamples.Spring.Domain.Aggregates.AuthProof.Events;


namespace MississippiSamples.Spring.Domain.Aggregates.AuthProof.Reducers;

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