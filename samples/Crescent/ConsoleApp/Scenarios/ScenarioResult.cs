using System.Collections.Generic;


namespace Crescent.ConsoleApp.Scenarios;

/// <summary>
///     Represents the result of a scenario execution.
/// </summary>
/// <remarks>
///     Provides a consistent contract for all scenario runners, enabling uniform
///     orchestration and reporting in Program.cs.
/// </remarks>
internal sealed record ScenarioResult
{
    /// <summary>
    ///     Gets optional additional data from the scenario.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Data { get; init; }

    /// <summary>
    ///     Gets the elapsed time in milliseconds.
    /// </summary>
    public int ElapsedMs { get; init; }

    /// <summary>
    ///     Gets a message describing the result.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the scenario passed.
    /// </summary>
    public required bool Passed { get; init; }

    /// <summary>
    ///     Gets the scenario name.
    /// </summary>
    public required string ScenarioName { get; init; }

    /// <summary>
    ///     Creates a failed result.
    /// </summary>
    /// <param name="scenarioName">The scenario name.</param>
    /// <param name="reason">The failure reason.</param>
    /// <param name="elapsedMs">Elapsed time in milliseconds.</param>
    /// <param name="data">Optional additional data.</param>
    /// <returns>A failing scenario result.</returns>
    public static ScenarioResult Failure(
        string scenarioName,
        string reason,
        int elapsedMs = 0,
        IReadOnlyDictionary<string, object>? data = null
    ) =>
        new()
        {
            Passed = false,
            ScenarioName = scenarioName,
            Message = reason,
            ElapsedMs = elapsedMs,
            Data = data,
        };

    /// <summary>
    ///     Creates a result from a boolean pass/fail indicator.
    /// </summary>
    /// <param name="passed">Whether the scenario passed.</param>
    /// <param name="scenarioName">The scenario name.</param>
    /// <param name="elapsedMs">Elapsed time in milliseconds.</param>
    /// <param name="message">Optional message.</param>
    /// <returns>A scenario result.</returns>
    public static ScenarioResult FromBool(
        bool passed,
        string scenarioName,
        int elapsedMs = 0,
        string? message = null
    ) =>
        new()
        {
            Passed = passed,
            ScenarioName = scenarioName,
            Message = message ?? (passed ? "Passed" : "Failed"),
            ElapsedMs = elapsedMs,
        };

    /// <summary>
    ///     Creates a successful result.
    /// </summary>
    /// <param name="scenarioName">The scenario name.</param>
    /// <param name="elapsedMs">Elapsed time in milliseconds.</param>
    /// <param name="message">Optional success message.</param>
    /// <param name="data">Optional additional data.</param>
    /// <returns>A passing scenario result.</returns>
    public static ScenarioResult Success(
        string scenarioName,
        int elapsedMs = 0,
        string? message = null,
        IReadOnlyDictionary<string, object>? data = null
    ) =>
        new()
        {
            Passed = true,
            ScenarioName = scenarioName,
            Message = message ?? "Passed",
            ElapsedMs = elapsedMs,
            Data = data,
        };
}