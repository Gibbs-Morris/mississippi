using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

using Mississippi.Inlet.Abstractions;


namespace Spring.Client.Features.TransferFundsSaga.Dtos;

/// <summary>
///     Client-side DTO for real-time TransferFunds saga status updates.
/// </summary>
/// <remarks>
///     <para>
///         This DTO mirrors the server-side <c>TransferFundsSagaStatusProjection</c>
///         and is used by Inlet to automatically fetch and cache saga status via SignalR.
///     </para>
///     <para>
///         The <see cref="ProjectionPathAttribute" /> must match the server projection's path
///         for Inlet to correctly route subscription requests.
///     </para>
/// </remarks>
[ProjectionPath("transfer-saga-status")]
public sealed record SagaStatusDto
{
    /// <summary>
    ///     Gets the unique identifier of the saga instance.
    /// </summary>
    [JsonPropertyName("sagaId")]
    public required string SagaId { get; init; }

    /// <summary>
    ///     Gets the type name of the saga.
    /// </summary>
    [JsonPropertyName("sagaType")]
    public required string SagaType { get; init; }

    /// <summary>
    ///     Gets the current phase of the saga (NotStarted, Running, Completed, Failed, Compensating).
    /// </summary>
    [JsonPropertyName("phase")]
    public required string Phase { get; init; }

    /// <summary>
    ///     Gets when the saga started, if it has started.
    /// </summary>
    [JsonPropertyName("startedAt")]
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>
    ///     Gets when the saga completed, if it has completed.
    /// </summary>
    [JsonPropertyName("completedAt")]
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    ///     Gets the currently executing step, if any.
    /// </summary>
    [JsonPropertyName("currentStep")]
    public SagaStepDto? CurrentStep { get; init; }

    /// <summary>
    ///     Gets the list of steps that have completed successfully.
    /// </summary>
    [JsonPropertyName("completedSteps")]
    public ImmutableArray<SagaStepDto> CompletedSteps { get; init; } =
        ImmutableArray<SagaStepDto>.Empty;

    /// <summary>
    ///     Gets the list of steps that have failed.
    /// </summary>
    [JsonPropertyName("failedSteps")]
    public ImmutableArray<SagaStepDto> FailedSteps { get; init; } =
        ImmutableArray<SagaStepDto>.Empty;

    /// <summary>
    ///     Gets the reason for saga failure, if the saga failed.
    /// </summary>
    [JsonPropertyName("failureReason")]
    public string? FailureReason { get; init; }

    /// <summary>
    ///     Gets the total number of steps in the saga.
    /// </summary>
    [JsonPropertyName("totalSteps")]
    public int TotalSteps { get; init; }
}
