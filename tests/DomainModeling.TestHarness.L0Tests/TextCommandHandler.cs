using System.Collections.Generic;

using Mississippi.DomainModeling.Abstractions;


namespace Mississippi.DomainModeling.TestHarness.L0Tests;

/// <summary>
///     Records the supplied state count in successful events and rejects a designated command.
/// </summary>
internal sealed class TextCommandHandler : CommandHandlerBase<string, List<string>>
{
    /// <inheritdoc />
    protected override OperationResult<IReadOnlyList<object>> HandleCore(
        string command,
        List<string>? state
    ) =>
        command == "reject"
            ? OperationResult.Fail<IReadOnlyList<object>>("rejected", "Cannot accept this command.")
            : OperationResult.Ok<IReadOnlyList<object>>([$"{state?.Count ?? 0}:{command}"]);
}