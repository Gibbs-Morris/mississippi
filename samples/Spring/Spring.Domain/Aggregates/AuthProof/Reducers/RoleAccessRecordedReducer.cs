using Mississippi.Spring.Domain.Aggregates.AuthProof.Events;
using Mississippi.Tributary.Abstractions;


namespace Mississippi.Spring.Domain.Aggregates.AuthProof.Reducers;

/// <summary>
///     Reducer for <see cref="RoleAccessRecorded" /> events.
/// </summary>
internal sealed class RoleAccessRecordedReducer : EventReducerBase<RoleAccessRecorded, AuthProofAggregate>
{
    /// <inheritdoc />
    protected override AuthProofAggregate ReduceCore(
        AuthProofAggregate state,
        RoleAccessRecorded @event
    ) =>
        (state ?? new()) with
        {
            RoleAccessCount = (state?.RoleAccessCount ?? 0) + 1,
        };
}