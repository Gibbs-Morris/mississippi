namespace Cascade.Web.Contracts;

/// <summary>
///     Request to convert a string to uppercase via the greeter grain.
/// </summary>
public sealed record ToUpperRequest
{
    /// <summary>
    ///     Gets the input string to convert.
    /// </summary>
    public required string Input { get; init; }
}