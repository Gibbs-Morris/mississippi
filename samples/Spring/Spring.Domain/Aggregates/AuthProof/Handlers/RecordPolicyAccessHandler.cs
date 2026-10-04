using System.Collections.Generic;

using Mississippi.DomainModeling.Abstractions;

using MississippiSamples.Spring.Domain.Aggregates.AuthProof.Commands;
using MississippiSamples.Spring.Domain.Aggregates.AuthProof.Events;


namespace MississippiSamples.Spring.Domain.Aggregates.AuthProof.Handlers;

/// <summary>
///     Handler for claim-policy auth-proof endpoint access.
/// </summary>
internal sealed class RecordPolicyAccessHandler : CommandHandlerBase<RecordPolicyAccess, AuthProofAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        RecordPolicyAccess command,
        AuthProofAggregate? state
    ) =>
        OperationResult.Ok<IReadOnlyList<object>>(new object[] { new PolicyAccessRecorded() });
}