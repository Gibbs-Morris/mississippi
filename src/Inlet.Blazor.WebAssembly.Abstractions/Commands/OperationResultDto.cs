namespace Mississippi.Inlet.Blazor.WebAssembly.Abstractions.Commands;

/// <summary>
///     Standard response DTO for aggregate command operation results.
/// </summary>
/// <remarks>
///     <para>
///         This DTO is returned from command endpoints to indicate success or failure
///         with optional error details. Effects use this to dispatch the appropriate
///         succeeded or failed lifecycle action.
///     </para>
/// </remarks>
/// <param name="Success">Whether the operation succeeded.</param>
/// <param name="ErrorCode">The error code if the operation failed, or <c>null</c> on success.</param>
/// <param name="ErrorMessage">The error message if the operation failed, or <c>null</c> on success.</param>
public sealed record OperationResultDto(bool Success, string? ErrorCode, string? ErrorMessage);