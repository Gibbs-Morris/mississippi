using System.Collections.Generic;

using Mississippi.DomainModeling.Abstractions;

using MississippiSamples.Spring.Domain.Aggregates.AuthProof.Commands;
using MississippiSamples.Spring.Domain.Aggregates.AuthProof.Events;


namespace MississippiSamples.Spring.Domain.Aggregates.AuthProof.Handlers;

/// <summary>
///     Handler for role-protected auth-proof endpoint access.
/// </summary>
internal sealed class RecordRoleAccessHandler : CommandHandlerBase<RecordRoleAccess, AuthProofAggregate>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        RecordRoleAccess command,
        AuthProofAggregate? state
    ) =>
        OperationResult.Ok<IReadOnlyList<object>>(new object[] { new RoleAccessRecorded() });
}