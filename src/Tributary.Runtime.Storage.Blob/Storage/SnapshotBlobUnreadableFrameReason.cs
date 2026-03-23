namespace Mississippi.Tributary.Runtime.Storage.Blob.Storage;

/// <summary>
///     Identifies why a stored Blob frame cannot be read as a valid snapshot.
/// </summary>
internal enum SnapshotBlobUnreadableFrameReason
{
    /// <summary>
    ///     The frame prelude is missing or truncated.
    /// </summary>
    TruncatedPrelude = 0,

    /// <summary>
    ///     The stored magic bytes do not match the Blob frame format.
    /// </summary>
    InvalidMagic = 1,

    /// <summary>
    ///     The stored frame version is not supported.
    /// </summary>
    UnsupportedFrameVersion = 2,

    /// <summary>
    ///     The stored frame flags are not supported.
    /// </summary>
    UnsupportedFlags = 3,

    /// <summary>
    ///     The stored header length is invalid or exceeds the configured limit.
    /// </summary>
    InvalidHeaderLength = 4,

    /// <summary>
    ///     The stored header JSON cannot be parsed.
    /// </summary>
    InvalidHeader = 5,

    /// <summary>
    ///     The stored header does not contain the expected required values.
    /// </summary>
    InvalidHeaderValues = 6,

    /// <summary>
    ///     The stored stream identity or snapshot version does not match the requested snapshot.
    /// </summary>
    UnexpectedSnapshotIdentity = 7,

    /// <summary>
    ///     The stored serializer identity is unknown to the current process.
    /// </summary>
    UnknownPayloadSerializer = 8,

    /// <summary>
    ///     The stored compression algorithm is unknown.
    /// </summary>
    UnknownCompressionAlgorithm = 9,

    /// <summary>
    ///     The stored payload bytes do not match the expected stored length.
    /// </summary>
    InvalidStoredPayloadLength = 10,

    /// <summary>
    ///     The compressed payload bytes could not be decompressed.
    /// </summary>
    InvalidCompressedPayload = 11,

    /// <summary>
    ///     The restored payload length does not match the stored metadata.
    /// </summary>
    InvalidUncompressedPayloadLength = 12,

    /// <summary>
    ///     The restored payload checksum does not match the stored checksum.
    /// </summary>
    PayloadChecksumMismatch = 13,
}