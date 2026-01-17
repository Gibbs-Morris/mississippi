using System;


namespace Mississippi.DocumentationGenerator;

/// <summary>
///     Exception thrown by the documentation generator for actionable errors.
/// </summary>
public sealed class DocGenException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DocGenException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DocGenException(
        string message
    )
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DocGenException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public DocGenException(
        string message,
        Exception innerException
    )
        : base(message, innerException)
    {
    }
}
