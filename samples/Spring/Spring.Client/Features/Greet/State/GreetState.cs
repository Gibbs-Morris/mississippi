using System;

using Mississippi.Reservoir.Abstractions.State;


namespace Spring.Client.Features.Greet.State;

/// <summary>
///     Feature state for the greeting functionality.
/// </summary>
internal sealed record GreetState : IFeatureState
{
    /// <inheritdoc />
    public static string FeatureKey => "greet";

    /// <summary>
    ///     Gets the error message if the greeting request failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    ///     Gets the timestamp when the greeting was generated.
    /// </summary>
    public DateTime? GeneratedAt { get; init; }

    /// <summary>
    ///     Gets the greeting message.
    /// </summary>
    public string? Greeting { get; init; }

    /// <summary>
    ///     Gets a value indicating whether a greeting request is in progress.
    /// </summary>
    public bool IsLoading { get; init; }
}