using System;

using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.DomainModeling.Abstractions;

/// <summary>
///     Event emitted when manual continuation of a running saga is requested.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.DomainModeling.Abstractions.SagaResumeRequested")]
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGARESUMEREQUESTED", 1)]
public sealed record SagaResumeRequested
{
    /// <summary>
    ///     Gets the optional correlation identifier.
    /// </summary>
    [Id(1)]
    public string? CorrelationId { get; init; }

    /// <summary>
    ///     Gets the timestamp when resume was requested.
    /// </summary>
    [Id(2)]
    public required DateTimeOffset RequestedAt { get; init; }

    /// <summary>
    ///     Gets the saga identifier.
    /// </summary>
    [Id(0)]
    public required Guid SagaId { get; init; }
}