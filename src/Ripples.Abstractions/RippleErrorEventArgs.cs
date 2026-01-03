namespace Mississippi.Ripples.Abstractions;

using System;

/// <summary>
/// Event arguments for ripple errors.
/// </summary>
public sealed class RippleErrorEventArgs : EventArgs
{
    /// <summary>
    /// Gets the exception that occurred.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RippleErrorEventArgs"/> class.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    public RippleErrorEventArgs(Exception exception)
    {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }
}
