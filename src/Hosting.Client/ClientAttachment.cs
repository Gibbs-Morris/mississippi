namespace Mississippi.Hosting.Client;

/// <summary>
///     Tracks the attachment and unrecoverable failure state of one client host.
/// </summary>
internal sealed class ClientAttachment
{
    /// <summary>
    ///     Gets or sets a value indicating whether the host service graph cannot be safely reused.
    /// </summary>
    public bool IsDamaged { get; set; }
}