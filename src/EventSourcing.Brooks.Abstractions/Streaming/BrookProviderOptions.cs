using Mississippi.Common.Abstractions;


namespace Mississippi.EventSourcing.Brooks.Abstractions.Streaming;

/// <summary>
///     Configuration options for the brook provider that interfaces with Orleans streams.
///     Contains settings for Orleans stream provider configuration.
/// </summary>
public sealed class BrookProviderOptions
{
    /// <summary>
    ///     Gets or sets the name of the Orleans stream provider used for brook operations.
    ///     Default value is <see cref="MississippiDefaults.StreamProviderName" />.
    /// </summary>
    /// <value>The name of the Orleans stream provider.</value>
    public string OrleansStreamProviderName { get; set; } = MississippiDefaults.StreamProviderName;
}