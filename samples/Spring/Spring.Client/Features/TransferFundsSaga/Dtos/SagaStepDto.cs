using System;
using System.Text.Json.Serialization;


namespace Spring.Client.Features.TransferFundsSaga.Dtos;

/// <summary>
///     Represents information about a saga step.
/// </summary>
public sealed record SagaStepDto
{
    /// <summary>
    ///     Gets the error message if the step failed.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Gets the outcome of the step.
    /// </summary>
    [JsonPropertyName("outcome")]
    public string Outcome { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the name of the step.
    /// </summary>
    [JsonPropertyName("stepName")]
    public required string StepName { get; init; }

    /// <summary>
    ///     Gets the execution order of the step.
    /// </summary>
    [JsonPropertyName("stepOrder")]
    public int StepOrder { get; init; }

    /// <summary>
    ///     Gets when the step reached its current outcome.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; }
}