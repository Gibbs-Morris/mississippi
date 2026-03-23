using Mississippi.Brooks.Serialization.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Blob.Startup;

/// <summary>
///     Describes the configured snapshot payload serializer and its persisted identity.
/// </summary>
/// <param name="Provider">The resolved serialization provider.</param>
/// <param name="SerializerId">The concrete serializer identity persisted in stored Blob headers.</param>
internal sealed record SnapshotPayloadSerializerDescriptor(
    ISerializationProvider Provider,
    string SerializerId);