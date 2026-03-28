using System;
using System.Security.Cryptography;
using System.Text;

using Mississippi.Brooks.Abstractions;


namespace Mississippi.Brooks.Runtime.Storage.Azure.Storage;

/// <summary>
///     Encodes Brooks Azure blob paths using a deterministic SHA-256 stream prefix.
/// </summary>
internal sealed class Sha256StreamPathEncoder : IStreamPathEncoder
{
    /// <inheritdoc />
    public string GetCursorPath(
        BrookKey brookId
    ) =>
        $"{GetStreamPrefix(brookId)}/cursor.json";

    /// <inheritdoc />
    public string GetEventPath(
        BrookKey brookId,
        long position
    ) =>
        $"{GetStreamPrefix(brookId)}/events/{position:D20}.json";

    /// <inheritdoc />
    public string GetLockPath(
        BrookKey brookId
    ) =>
        $"{GetStreamPrefix(brookId)}.lock";

    /// <inheritdoc />
    public string GetPendingPath(
        BrookKey brookId
    ) =>
        $"{GetStreamPrefix(brookId)}/pending.json";

    private static string GetStreamPrefix(
        BrookKey brookId
    )
    {
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(brookId.ToString()));
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