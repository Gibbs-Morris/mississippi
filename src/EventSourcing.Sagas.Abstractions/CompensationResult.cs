namespace Mississippi.EventSourcing.Sagas.Abstractions;

public sealed record CompensationResult
{
    public bool Success { get; init; }
    public bool Skipped { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static CompensationResult Succeeded() => new() { Success = true };
    public static CompensationResult Skipped(string? reason = null) => new() { Skipped = true };
    public static CompensationResult Failed(string errorCode, string? message = null) =>
        new() { Success = false, ErrorCode = errorCode, ErrorMessage = message };
}
