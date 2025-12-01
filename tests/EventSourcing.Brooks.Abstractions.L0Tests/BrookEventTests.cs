using System;
using System.Collections.Immutable;


namespace Mississippi.EventSourcing.Abstractions.Tests;

/// <summary>
///     Tests for the <see cref="BrookEvent" /> record.
/// </summary>
public class BrookEventTests
{
    /// <summary>
    ///     Default-constructed values are empty or null as defined.
    /// </summary>
    [Fact]
    public void DefaultValuesAreEmptyOrNull()
    {
        BrookEvent e = new();
        Assert.Equal(string.Empty, e.Type);
        Assert.Equal(string.Empty, e.Source);
        Assert.Equal(string.Empty, e.Id);
        Assert.Equal(string.Empty, e.DataContentType);
        Assert.True(e.Data.IsDefaultOrEmpty);
        Assert.Null(e.Time);
    }

    /// <summary>
    ///     Record initializer sets provided values.
    /// </summary>
    [Fact]
    public void InitializeWithValues()
    {
        ImmutableArray<byte> data = ImmutableArray.Create((byte)1, (byte)2);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        BrookEvent e = new()
        {
            Type = "T",
            Source = "S",
            Id = "I",
            DataContentType = "application/json",
            Data = data,
            Time = now,
        };
        Assert.Equal("T", e.Type);
        Assert.Equal("S", e.Source);
        Assert.Equal("I", e.Id);
        Assert.Equal("application/json", e.DataContentType);
        Assert.Equal(data, e.Data);
        Assert.Equal(now, e.Time);
    }
}