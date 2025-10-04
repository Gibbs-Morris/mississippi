namespace Mississippi.EventSourcing.Reader;

/// <summary>
///     Configuration options for the brook provider that interfaces with Orleans streams.
///     Contains settings for Orleans stream provider configuration.
/// </summary>
public class BrookProviderOptions
{
    /// <summary>
    ///     Gets or initializes the name of the Orleans stream provider used for brook operations.
    ///     Default value is "MississippiBrookStreamProvider".
    /// </summary>
    /// <value>The name of the Orleans stream provider.</value>
    public string OrleansStreamProviderName { get; init; } = "MississippiBrookStreamProvider";
}
