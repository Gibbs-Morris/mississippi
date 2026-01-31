namespace Mississippi.Sdk.Silo;

/// <summary>
///     Options for configuring in-memory Orleans providers.
/// </summary>
public sealed class InMemoryProviderOptions
{
    /// <summary>
    ///     Gets or sets the grain storage provider names.
    /// </summary>
    public StorageProviderNames StorageNames { get; set; } = new();

    /// <summary>
    ///     Gets or sets the Orleans stream provider name.
    /// </summary>
    public string StreamProviderName { get; set; } = "mississippi-streaming";
}