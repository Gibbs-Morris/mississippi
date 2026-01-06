namespace Cascade.WebApi.Controllers.Contracts;

/// <summary>
///     Error response.
/// </summary>
/// <param name="Message">The error message.</param>
public sealed record ErrorResponse(string Message);
