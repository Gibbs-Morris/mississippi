namespace Mississippi.Hosting.Runtime;

/// <summary>
///     Tracks the attachment and unrecoverable failure state of one runtime host.
/// </summary>
internal sealed class RuntimeAttachment
{
    /// <summary>
    ///     Gets or sets a value indicating whether the host service graph cannot be safely reused.
    /// </summary>
    public bool IsDamaged { get; set; }
}