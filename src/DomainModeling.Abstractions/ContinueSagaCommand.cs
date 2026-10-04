using System;

using Orleans;


namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Command that requests manual saga resume from a client or UX action.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.DomainModeling.Abstractions.ContinueSagaCommand")]
public sealed record ContinueSagaCommand
{
    /// <summary>
    ///     Gets the optional correlation identifier.
    /// </summary>
    [Id(1)]
    public string? CorrelationId { get; init; }

    /// <summary>
    ///     Gets the saga identifier.
    /// </summary>
    [Id(0)]
    public required Guid SagaId { get; init; }
}
