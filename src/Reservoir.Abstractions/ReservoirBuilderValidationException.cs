using System;


namespace Mississippi.Reservoir.Abstractions;

/// <summary>
///     Represents a Reservoir builder configuration error.
/// </summary>
public sealed class ReservoirBuilderValidationException : InvalidOperationException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReservoirBuilderValidationException" /> class.
    /// </summary>
    public ReservoirBuilderValidationException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReservoirBuilderValidationException" /> class.
    /// </summary>
    /// <param name="message">The validation error message.</param>
    public ReservoirBuilderValidationException(
        string message
    )
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ReservoirBuilderValidationException" /> class.
    /// </summary>
    /// <param name="message">The validation error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ReservoirBuilderValidationException(
        string message,
        Exception innerException
    )
        : base(message, innerException)
    {
    }
}