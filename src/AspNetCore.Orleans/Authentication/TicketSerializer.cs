using System;
using Microsoft.AspNetCore.Authentication;

namespace Mississippi.AspNetCore.Orleans.Authentication;

/// <summary>
/// Serializes and deserializes <see cref="AuthenticationTicket"/> instances using the built-in ticket serializer.
/// </summary>
public sealed class TicketSerializer
{
    private static readonly Microsoft.AspNetCore.Authentication.TicketSerializer InternalSerializer =
        Microsoft.AspNetCore.Authentication.TicketSerializer.Default;

    /// <summary>
    /// Serializes an authentication ticket to a byte array.
    /// </summary>
    /// <param name="ticket">The ticket to serialize.</param>
    /// <returns>The serialized ticket bytes.</returns>
    public byte[] Serialize(AuthenticationTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        return InternalSerializer.Serialize(ticket);
    }

    /// <summary>
    /// Deserializes an authentication ticket from a byte array.
    /// </summary>
    /// <param name="ticketBytes">The ticket bytes to deserialize.</param>
    /// <returns>The deserialized authentication ticket, or null if deserialization fails.</returns>
    public AuthenticationTicket? Deserialize(byte[] ticketBytes)
    {
        ArgumentNullException.ThrowIfNull(ticketBytes);
        return InternalSerializer.Deserialize(ticketBytes);
    }
}


