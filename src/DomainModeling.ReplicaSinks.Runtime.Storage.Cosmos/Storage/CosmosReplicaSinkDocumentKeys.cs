using System;
using System.Globalization;

using Newtonsoft.Json;


namespace Mississippi.DomainModeling.ReplicaSinks.Runtime.Storage.Cosmos.Storage;

/// <summary>
///     Defines internal document identifiers and serialization helpers for Cosmos-backed replica sinks.
/// </summary>
internal static class CosmosReplicaSinkDocumentKeys
{
    /// <summary>
    ///     The document discriminator used for durable delivery-state documents.
    /// </summary>
    internal const string DeliveryStateDocumentType = "state";

    /// <summary>
    ///     The document discriminator used for target-delivery documents.
    /// </summary>
    internal const string TargetDeliveryDocumentType = "delivery";

    /// <summary>
    ///     The document discriminator used for provisioned target markers.
    /// </summary>
    internal const string TargetMarkerDocumentType = "target";

    /// <summary>
    ///     The identifier used for target-marker documents within a target partition.
    /// </summary>
    internal const string TargetMarkerId = "target";

    /// <summary>
    ///     Creates the durable delivery-state document identifier for the supplied delivery key.
    /// </summary>
    /// <param name="deliveryKey">The runtime-owned delivery key.</param>
    /// <returns>The durable delivery-state document identifier.</returns>
    internal static string DeliveryStateId(
        string deliveryKey
    ) => $"state::{deliveryKey}";

    /// <summary>
    ///     Serializes a UTC timestamp using the round-trip format required by Cosmos string ordering.
    /// </summary>
    /// <param name="timestamp">The timestamp to serialize.</param>
    /// <returns>The round-trip UTC timestamp.</returns>
    internal static string FormatUtcTimestamp(
        DateTimeOffset timestamp
    ) => timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    /// <summary>
    ///     Parses an optional UTC timestamp stored in a Cosmos document.
    /// </summary>
    /// <param name="value">The stored timestamp value.</param>
    /// <returns>The parsed timestamp, or <see langword="null" /> when no value is stored.</returns>
    internal static DateTimeOffset? ParseNullableUtcTimestamp(
        string? value
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    /// <summary>
    ///     Deserializes a stored payload snapshot from its JSON representation.
    /// </summary>
    /// <param name="payloadJson">The stored payload JSON.</param>
    /// <returns>The deserialized payload, or <see langword="null" /> when no payload is present.</returns>
    internal static object? DeserializePayload(
        string? payloadJson
    ) => payloadJson is null ? null : JsonConvert.DeserializeObject<object>(payloadJson);

    /// <summary>
    ///     Serializes a payload snapshot for Cosmos storage.
    /// </summary>
    /// <param name="payload">The payload to serialize.</param>
    /// <returns>The serialized payload, or <see langword="null" /> when no payload is present.</returns>
    internal static string? SerializePayload(
        object? payload
    ) => payload is null ? null : JsonConvert.SerializeObject(payload);

    /// <summary>
    ///     Creates the target-delivery document identifier for the supplied delivery key.
    /// </summary>
    /// <param name="deliveryKey">The runtime-owned delivery key.</param>
    /// <returns>The target-delivery document identifier.</returns>
    internal static string TargetDeliveryId(
        string deliveryKey
    ) => $"delivery::{deliveryKey}";
}
