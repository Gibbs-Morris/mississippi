namespace Mississippi.Hosting.Runtime;

/// <summary>
///     Marks a host whose runtime attachment is reserved or complete.
/// </summary>
internal sealed class RuntimeAttachment
{
    private RuntimeAttachment()
    {
    }

    /// <summary>
    ///     Gets the immutable marker used by host-specific runtime attachment descriptors.
    /// </summary>
    public static RuntimeAttachment Instance { get; } = new();
}