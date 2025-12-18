using System;
using System.Collections.Immutable;
using System.Security.Cryptography;

using Mississippi.EventSourcing.Abstractions;


namespace Crescent.ConsoleApp.Shared;

/// <summary>
///     Factory methods to generate synthetic <see cref="BrookEvent" /> payloads for sample scenarios.
/// </summary>
internal static class SampleEventFactory
{
    private static readonly string[] Categories = { "Metric", "Alert", "Heartbeat", "Audit", "Diagnostic" };

    private static readonly string[] MimeTypes =
    {
        "application/json", "text/plain", "application/xml", "application/octet-stream",
    };

    /// <summary>
    ///     Creates a set of random events with variable payload sizes and metadata.
    /// </summary>
    /// <param name="count">The number of events to create.</param>
    /// <returns>An immutable array containing the generated events.</returns>
    public static ImmutableArray<BrookEvent> CreateEvents(
        int count = 10
    )
    {
        ImmutableArray<BrookEvent>.Builder builder = ImmutableArray.CreateBuilder<BrookEvent>(count);
        for (int i = 0; i < count; i++)
        {
            builder.Add(CreateRandomEvent());
        }

        return builder.MoveToImmutable();
    }

    /// <summary>
    ///     Creates a set of events with a fixed payload size.
    /// </summary>
    /// <param name="count">The number of events to create.</param>
    /// <param name="sizeBytes">The exact payload size in bytes for each event.</param>
    /// <param name="contentType">Optional content type; defaults to application/octet-stream.</param>
    /// <returns>An immutable array containing the generated events.</returns>
    public static ImmutableArray<BrookEvent> CreateFixedSizeEvents(
        int count,
        int sizeBytes,
        string? contentType = null
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sizeBytes, 1);
        ImmutableArray<BrookEvent>.Builder builder = ImmutableArray.CreateBuilder<BrookEvent>(count);
        for (int i = 0; i < count; i++)
        {
            builder.Add(CreateEventOfSize(sizeBytes, contentType));
        }

        return builder.MoveToImmutable();
    }

    /// <summary>
    ///     Creates a set of events with random payload sizes within the provided range.
    /// </summary>
    /// <param name="count">The number of events to create.</param>
    /// <param name="minBytes">The inclusive lower bound for payload size.</param>
    /// <param name="maxBytes">The inclusive upper bound for payload size.</param>
    /// <param name="contentType">Optional content type; defaults to application/octet-stream.</param>
    /// <returns>An immutable array containing the generated events.</returns>
    public static ImmutableArray<BrookEvent> CreateRangeSizeEvents(
        int count,
        int minBytes,
        int maxBytes,
        string? contentType = null
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minBytes, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minBytes, maxBytes);
        ImmutableArray<BrookEvent>.Builder builder = ImmutableArray.CreateBuilder<BrookEvent>(count);
        for (int i = 0; i < count; i++)
        {
            int size = RandomNumberGenerator.GetInt32(minBytes, maxBytes + 1);
            builder.Add(CreateEventOfSize(size, contentType));
        }

        return builder.MoveToImmutable();
    }

    private static BrookEvent CreateEventOfSize(
        int sizeBytes,
        string? contentType
    )
    {
        byte[] payload = new byte[sizeBytes];
        RandomNumberGenerator.Fill(payload);
        return new()
        {
            Id = Guid.NewGuid().ToString(),
            Data = payload.ToImmutableArray(),
            DataContentType = contentType ?? "application/octet-stream",
            Source = GenerateSourceTag(),
            Time = DateTimeOffset.UtcNow,
            EventType = Pick(Categories),
        };
    }

    private static BrookEvent CreateRandomEvent()
    {
        int payloadSize = RandomNumberGenerator.GetInt32(512, 4_096);
        byte[] payload = new byte[payloadSize];
        RandomNumberGenerator.Fill(payload);
        return new()
        {
            Id = Guid.NewGuid().ToString(),
            Data = payload.ToImmutableArray(),
            DataContentType = Pick(MimeTypes),
            Source = GenerateSourceTag(),
            Time = DateTimeOffset.UtcNow.AddSeconds(-RandomNumberGenerator.GetInt32(0, 86_400)),
            EventType = Pick(Categories),
        };
    }

    private static string GenerateSourceTag()
    {
        const string pool = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        Span<char> tag = stackalloc char[5];
        for (int i = 0; i < tag.Length; i++)
        {
            tag[i] = pool[RandomNumberGenerator.GetInt32(pool.Length)];
        }

        return $"SRC-{new string(tag)}";
    }

    private static T Pick<T>(
        T[] array
    ) =>
        array[RandomNumberGenerator.GetInt32(array.Length)];
}