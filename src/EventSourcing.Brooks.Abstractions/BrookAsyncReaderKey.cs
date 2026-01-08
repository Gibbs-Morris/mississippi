using System;

using Orleans;


namespace Mississippi.EventSourcing.Brooks.Abstractions;

/// <summary>
///     Represents a unique key for identifying a brook async reader grain instance.
///     Each instance includes a random suffix to ensure single-use semantics.
/// </summary>
/// <remarks>
///     <para>
///         This key type is used to create unique grain instances for streaming reads.
///         The random suffix ensures that each streaming request gets its own grain activation,
///         avoiding the <c>[StatelessWorker]</c> + <c>IAsyncEnumerable</c> incompatibility issue
///         while still achieving practical parallelism.
///     </para>
///     <para>
///         The grain identified by this key will be garbage-collected by Orleans'
///         idle deactivation policy after use.
///     </para>
/// </remarks>
[GenerateSerializer]
[Alias("Mississippi.EventSourcing.Brooks.Abstractions.BrookAsyncReaderKey")]
public readonly record struct BrookAsyncReaderKey
{
    private const char Separator = '|';

    /// <summary>
    ///     Initializes a new instance of the <see cref="BrookAsyncReaderKey" /> struct.
    /// </summary>
    /// <param name="brookKey">The underlying brook key identifying the event stream.</param>
    /// <param name="instanceId">A unique instance identifier to ensure single-use grain semantics.</param>
    public BrookAsyncReaderKey(
        BrookKey brookKey,
        Guid instanceId
    )
    {
        BrookKey = brookKey;
        InstanceId = instanceId;
    }

    /// <summary>
    ///     Gets the underlying brook key that identifies the event stream.
    /// </summary>
    [Id(0)]
    public BrookKey BrookKey { get; }

    /// <summary>
    ///     Gets the unique instance identifier for this reader grain.
    /// </summary>
    [Id(1)]
    public Guid InstanceId { get; }

    /// <summary>
    ///     Creates a new async reader key for the specified brook with a random instance ID.
    /// </summary>
    /// <param name="brookKey">The brook key identifying the event stream.</param>
    /// <returns>A new async reader key with a unique instance identifier.</returns>
    public static BrookAsyncReaderKey Create(
        BrookKey brookKey
    ) =>
        new(brookKey, Guid.NewGuid());

    /// <summary>
    ///     Converts a string to a <see cref="BrookAsyncReaderKey" />.
    ///     This is the alternate method for the implicit string operator (CA2225).
    /// </summary>
    /// <param name="key">The string representation of the key.</param>
    /// <returns>The parsed async reader key.</returns>
    public static BrookAsyncReaderKey FromString(
        string key
    ) =>
        Parse(key);

    /// <summary>
    ///     Parses a string representation into a <see cref="BrookAsyncReaderKey" />.
    ///     This is the alternate method for the implicit string operator (CA2225).
    /// </summary>
    /// <param name="key">The string representation of the key.</param>
    /// <returns>The parsed async reader key.</returns>
    /// <exception cref="ArgumentException">Thrown when the string is not a valid async reader key format.</exception>
    public static BrookAsyncReaderKey Parse(
        string key
    )
    {
        ReadOnlySpan<char> span = key.AsSpan();
        int firstSep = span.IndexOf(Separator);
        if (firstSep < 0)
        {
            throw new ArgumentException("Invalid BrookAsyncReaderKey format: missing first separator.", nameof(key));
        }

        ReadOnlySpan<char> remaining = span[(firstSep + 1)..];
        int secondSep = remaining.IndexOf(Separator);
        if (secondSep < 0)
        {
            throw new ArgumentException("Invalid BrookAsyncReaderKey format: missing second separator.", nameof(key));
        }

        string brookName = span[..firstSep].ToString();
        string entityId = remaining[..secondSep].ToString();
        string instanceIdStr = remaining[(secondSep + 1)..].ToString();
        if (!Guid.TryParse(instanceIdStr, out Guid instanceId))
        {
            throw new ArgumentException("Invalid BrookAsyncReaderKey format: invalid instance ID.", nameof(key));
        }

        return new(new(brookName, entityId), instanceId);
    }

    /// <summary>
    ///     Converts the async reader key to its string representation for use as a grain key.
    /// </summary>
    /// <param name="key">The async reader key to convert.</param>
    /// <returns>A string representation in the format "brookName|entityId|instanceId".</returns>
    public static implicit operator string(
        BrookAsyncReaderKey key
    ) =>
        $"{key.BrookKey.BrookName}{Separator}{key.BrookKey.EntityId}{Separator}{key.InstanceId:N}";

    /// <summary>
    ///     Converts a string to a <see cref="BrookAsyncReaderKey" />.
    ///     Calls <see cref="Parse" /> internally.
    /// </summary>
    /// <param name="key">The string representation of the key.</param>
    /// <returns>The parsed async reader key.</returns>
    public static implicit operator BrookAsyncReaderKey(
        string key
    ) =>
        Parse(key);

    /// <summary>
    ///     Returns the string representation of this async reader key.
    /// </summary>
    /// <returns>A string representation in the format "brookName|entityId|instanceId".</returns>
    public override string ToString() => this;
}