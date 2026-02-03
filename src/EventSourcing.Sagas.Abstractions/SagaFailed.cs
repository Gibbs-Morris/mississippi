using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;

using Orleans;

namespace Mississippi.EventSourcing.Sagas.Abstractions;

/// <summary>
///     Event emitted when a saga fails.
/// </summary>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Sagas.Abstractions.SagaFailed")]
[EventStorageName("MISSISSIPPI", "SAGAS", "SAGAFAILED")]
public sealed record SagaFailed
{
    /// <summary>
    ///     Gets the error code describing the failure.
    /// </summary>
    [Id(0)]
    public required string ErrorCode { get; init; }

    /// <summary>
    ///     Gets the optional error message describing the failure.
    /// </summary>
    [Id(1)]
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Gets the timestamp when the saga failed.
    /// </summary>
    [Id(2)]
    public required DateTimeOffset FailedAt { get; init; }
}
