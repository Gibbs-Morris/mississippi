using System;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime;

/// <summary>
///     Parses runtime-owned delivery keys into their logical binding/entity components.
/// </summary>
internal static class ReplicaSinkDeliveryKeyParser
{
    public static ParsedReplicaSinkDeliveryKey Parse(
        string deliveryKey
    )
    {
        ArgumentNullException.ThrowIfNull(deliveryKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(deliveryKey);
        string[] parts = deliveryKey.Split(["::"], 4, StringSplitOptions.None);
        if (parts.Length != 4)
        {
            throw new InvalidOperationException("Replica sink delivery key format was invalid.");
        }

        return new(parts[0], parts[1], parts[2], parts[3]);
    }

    internal readonly record struct ParsedReplicaSinkDeliveryKey(
        string ProjectionTypeName,
        string SinkKey,
        string TargetName,
        string EntityId
    );
}
