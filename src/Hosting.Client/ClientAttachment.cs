namespace Mississippi.Hosting.Client;

/// <summary>
///     Marks a host whose Mississippi client composition has completed.
/// </summary>
internal sealed class ClientAttachment
{
    private ClientAttachment()
    {
    }

    /// <summary>
    ///     Gets the immutable marker shared by host-specific attachment descriptors.
    /// </summary>
    public static ClientAttachment Instance { get; } = new();
}