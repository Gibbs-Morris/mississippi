using Mississippi.Tributary.Abstractions;

using Spring.Domain.Aggregates.AuthProof.Events;


namespace Spring.Domain.Aggregates.AuthProof.Reducers;

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