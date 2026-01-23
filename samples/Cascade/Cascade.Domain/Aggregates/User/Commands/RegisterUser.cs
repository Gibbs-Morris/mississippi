using Mississippi.Sdk.Generators.Abstractions;

using Orleans;


namespace Cascade.Domain.Aggregates.User.Commands;

/// <summary>
///     Command to register a new user.
/// </summary>
[GenerateCommand(Route = "register")]
[GenerateSerializer]
[Alias("Cascade.Domain.User.Commands.RegisterUser")]
internal sealed record RegisterUser
{
    /// <summary>
    ///     Gets the user's display name.
    /// </summary>
    [Id(1)]
    public required string DisplayName { get; init; }

    /// <summary>
    ///     Gets the unique identifier for the user.
    /// </summary>
    [Id(0)]
    public required string UserId { get; init; }
}