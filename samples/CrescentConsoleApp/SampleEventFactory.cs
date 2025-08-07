using System.Collections.Immutable;
using System.Security.Cryptography;

using Mississippi.EventSourcing.Abstractions;


namespace Mississippi.CrescentConsoleApp;

public static class SampleEventFactory
{
    private static readonly string[] MimeTypes =
    {
        "application/json", "text/plain", "application/xml", "application/octet-stream",
    };

    private static readonly string[] Categories = { "Metric", "Alert", "Heartbeat", "Audit", "Diagnostic" };

    private static readonly Random Rng = new(); // single RNG instance

    public static ImmutableArray<BrookEvent> CreateEvents(
        int count = 10
    )
    {
        ImmutableArray<BrookEvent>.Builder builder = ImmutableArray.CreateBuilder<BrookEvent>(count);
        for (int i = 0; i < count; i++)
        {
            builder.Add(CreateRandomEvent());
        }

        return builder.MoveToImmutable(); // O(1) finalise
    }

    public static ImmutableArray<BrookEvent> CreateFixedSizeEvents(
        int count,
        int sizeBytes,
        string? contentType = null
    )
    {
        if (sizeBytes < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes));
        }

        ImmutableArray<BrookEvent>.Builder builder = ImmutableArray.CreateBuilder<BrookEvent>(count);
        for (int i = 0; i < count; i++)
        {
            builder.Add(CreateEventOfSize(sizeBytes, contentType));
        }

        return builder.MoveToImmutable();
    }

    public static ImmutableArray<BrookEvent> CreateRangeSizeEvents(
        int count,
        int minBytes,
        int maxBytes,
        string? contentType = null
    )
    {
        if ((minBytes < 1) || (maxBytes < minBytes))
        {
            throw new ArgumentOutOfRangeException(nameof(minBytes));
        }

        ImmutableArray<BrookEvent>.Builder builder = ImmutableArray.CreateBuilder<BrookEvent>(count);
        for (int i = 0; i < count; i++)
        {
            int size = Rng.Next(minBytes, maxBytes + 1);
            builder.Add(CreateEventOfSize(size, contentType));
        }

        return builder.MoveToImmutable();
    }

    private static BrookEvent CreateRandomEvent()
    {
        byte[] payload = new byte[Rng.Next(512, 4_096)];
        RandomNumberGenerator.Fill(payload);
        return new()
        {
            Id = Guid.NewGuid().ToString(),
            Data = payload.ToImmutableArray(),
            DataContentType = Pick(MimeTypes),
            Source = GenerateSourceTag(),
            Time = DateTimeOffset.UtcNow.AddSeconds(-Rng.Next(0, 86_400)),
            Type = Pick(Categories),
        };
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
            Type = Pick(Categories),
        };
    }

    private static string GenerateSourceTag()
    {
        const string pool = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        Span<char> tag = stackalloc char[5];
        for (int i = 0; i < tag.Length; i++)
        {
            tag[i] = pool[Rng.Next(pool.Length)];
        }

        return $"SRC-{new string(tag)}";
    }

    private static T Pick<T>(
        T[] array
    ) =>
        array[Rng.Next(array.Length)];
}