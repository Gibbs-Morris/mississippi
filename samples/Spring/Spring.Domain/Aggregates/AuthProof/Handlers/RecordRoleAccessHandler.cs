using System.Collections.Generic;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.Spring.Domain.Aggregates.AuthProof.Commands;
using Mississippi.Spring.Domain.Aggregates.AuthProof.Events;


namespace Mississippi.Spring.Domain.Aggregates.AuthProof.Handlers;

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