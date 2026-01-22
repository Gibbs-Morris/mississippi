using Mississippi.Sdk.Generators.Abstractions;


namespace Spring.Client.Features.BankAccountAggregate.Dtos;

/// <summary>
///     Response DTO for command operation results.
/// </summary>
/// <param name="Success">Whether the operation succeeded.</param>
/// <param name="ErrorCode">The error code if failed.</param>
/// <param name="ErrorMessage">The error message if failed.</param>
[PendingSourceGenerator]
internal sealed record OperationResultDto(bool Success, string? ErrorCode, string? ErrorMessage);