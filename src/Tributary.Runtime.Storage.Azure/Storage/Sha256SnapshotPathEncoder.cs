using System.Security.Cryptography;
using System.Text;

using Mississippi.Tributary.Abstractions;


namespace Mississippi.Tributary.Runtime.Storage.Azure.Storage;

/// <summary>
///     Encodes Tributary Azure blob paths using a deterministic SHA-256 snapshot stream prefix.
/// </summary>
internal sealed class Sha256SnapshotPathEncoder : ISnapshotPathEncoder
{
    /// <inheritdoc />
    public string GetSnapshotPath(
        SnapshotKey snapshotKey
    ) =>
        $"{GetStreamPrefix(snapshotKey.Stream)}/{snapshotKey.Version:D20}.json";

    /// <inheritdoc />
    public string GetStreamPrefix(
        SnapshotStreamKey streamKey
    )
    {
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(streamKey.ToString()));
        string hash = string.Create(hashBytes.Length * 2, hashBytes, static (span, bytes) =>
        {
            const string HexDigits = "0123456789abcdef";
            for (int index = 0; index < bytes.Length; index++)
            {
                byte value = bytes[index];
                span[index * 2] = HexDigits[value >> 4];
                span[(index * 2) + 1] = HexDigits[value & 0x0F];
            }
        });

        return $"streams/{hash[..2]}/{hash[2..4]}/{hash}";
    }
}
