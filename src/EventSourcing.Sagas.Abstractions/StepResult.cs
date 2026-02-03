using System.Collections.Generic;

namespace Mississippi.EventSourcing.Sagas.Abstractions;

public sealed record StepResult
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public IReadOnlyList<object> Events { get; init; } = [];

    public static StepResult Succeeded(params object[] events) =>
        new() { Success = true, Events = events };

    public static StepResult Failed(string errorCode, string? message = null) =>
        new() { Success = false, ErrorCode = errorCode, ErrorMessage = message };
}
