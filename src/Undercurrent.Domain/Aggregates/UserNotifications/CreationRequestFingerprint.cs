using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications.Commands;


namespace Mississippi.Undercurrent.Domain.Aggregates.UserNotifications;

/// <summary>Creates a versioned digest of exact request content and its fixed UTC expiry.</summary>
internal static class CreationRequestFingerprint
{
    /// <summary>Fingerprints the request without retaining another copy of notification content.</summary>
    /// <param name="command">The validated creation request.</param>
    /// <returns>The versioned SHA-256 digest.</returns>
    public static string Create(
        CreateNotification command
    )
    {
        using MemoryStream payload = new();
        using BinaryWriter writer = new(payload, Encoding.UTF8, true);
        writer.Write(1);
        WriteString(writer, command.Title);
        WriteString(writer, command.Description);
        WriteString(writer, command.IconKey);
        WriteString(writer, command.NavigationTarget);
        writer.Write(command.ExpiresAt.UtcTicks);
        writer.Flush();
        return "V1:" +
               Convert.ToHexString(SHA256.HashData(payload.GetBuffer().AsSpan(0, checked((int)payload.Length))));
    }

    private static void WriteString(
        BinaryWriter writer,
        string? value
    )
    {
        writer.Write(value is not null);
        if (value is null)
        {
            return;
        }

        writer.Write(value.Length);
        foreach (char character in value)
        {
            writer.Write((ushort)character);
        }
    }
}