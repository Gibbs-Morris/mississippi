using Mississippi.Spring.Domain.Aggregates.AuthProof.Events;
using Mississippi.Tributary.Abstractions;


namespace Mississippi.Spring.Domain.Aggregates.AuthProof.Reducers;

/// <summary>
///     Reducer for <see cref="PolicyAccessRecorded" /> events.
/// </summary>
internal sealed class PolicyAccessRecordedReducer : EventReducerBase<PolicyAccessRecorded, AuthProofAggregate>
{
    /// <inheritdoc />
    protected override AuthProofAggregate ReduceCore(
        AuthProofAggregate state,
        PolicyAccessRecorded @event
    ) =>
        (state ?? new()) with
        {
            ClaimPolicyAccessCount = (state?.ClaimPolicyAccessCount ?? 0) + 1,
        };
}