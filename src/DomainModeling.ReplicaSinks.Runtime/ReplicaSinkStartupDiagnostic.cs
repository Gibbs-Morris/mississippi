using System;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Represents a stable startup validation diagnostic for replica sink onboarding.
/// </summary>
internal sealed class ReplicaSinkStartupDiagnostic
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ReplicaSinkStartupDiagnostic" /> class.
    /// </summary>
    /// <param name="id">The stable diagnostic identifier.</param>
    /// <param name="message">The formatted diagnostic message.</param>
    public ReplicaSinkStartupDiagnostic(
        string id,
        string message
    )
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Id = id;
        Message = message;
    }

    /// <summary>
    ///     Gets the stable diagnostic identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    ///     Gets the formatted diagnostic message.
    /// </summary>
    public string Message { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Id}: {Message}";
}