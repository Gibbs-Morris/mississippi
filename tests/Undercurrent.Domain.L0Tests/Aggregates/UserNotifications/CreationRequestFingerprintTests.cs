using System;

using Mississippi.Undercurrent.Abstractions.Aggregates.UserNotifications.Commands;
using Mississippi.Undercurrent.Domain.Aggregates.UserNotifications;


namespace MississippiTests.Undercurrent.Domain.L0Tests.Aggregates.UserNotifications;

/// <summary>Verifies exact request identity across ambiguous strings, nullable fields, and offset-equivalent expiry.</summary>
public sealed class CreationRequestFingerprintTests
{
    /// <summary>Equivalent instants have one fingerprint while different deadlines remain different requests.</summary>
    [Fact]
    public void ExpiryUsesUtcInstant()
    {
        CreateNotification original = UserNotificationsFixture.CreateCommand();
        Assert.Equal(
            CreationRequestFingerprint.Create(original),
            CreationRequestFingerprint.Create(
                original with
                {
                    ExpiresAt = original.ExpiresAt.ToOffset(TimeSpan.FromHours(2)),
                }));
        Assert.NotEqual(
            CreationRequestFingerprint.Create(original),
            CreationRequestFingerprint.Create(
                original with
                {
                    ExpiresAt = original.ExpiresAt.AddTicks(1),
                }));
    }

    /// <summary>Different field boundaries cannot collide through simple concatenation.</summary>
    [Fact]
    public void FieldBoundariesAndNullableValuesRemainDistinct()
    {
        CreateNotification first = UserNotificationsFixture.CreateCommand() with
        {
            Title = "a",
            Description = "bc",
        };
        CreateNotification second = first with
        {
            Title = "ab",
            Description = "c",
        };
        Assert.NotEqual(CreationRequestFingerprint.Create(first), CreationRequestFingerprint.Create(second));
        Assert.NotEqual(
            CreationRequestFingerprint.Create(
                first with
                {
                    Description = null,
                }),
            CreationRequestFingerprint.Create(
                first with
                {
                    Description = string.Empty,
                }));
        string fingerprint = CreationRequestFingerprint.Create(first);
        Assert.StartsWith("V1:", fingerprint, StringComparison.Ordinal);
        Assert.Equal(67, fingerprint.Length);
    }

    /// <summary>Exact UTF-16 values are preserved, including values that UTF-8 replacement would merge.</summary>
    [Fact]
    public void Utf16CodeUnitsAndWhitespaceRemainDistinct()
    {
        CreateNotification request = UserNotificationsFixture.CreateCommand();
        Assert.NotEqual(
            CreationRequestFingerprint.Create(
                request with
                {
                    Description = "\uD800",
                }),
            CreationRequestFingerprint.Create(
                request with
                {
                    Description = "\uD801",
                }));
        Assert.NotEqual(
            CreationRequestFingerprint.Create(
                request with
                {
                    Title = "Ready",
                }),
            CreationRequestFingerprint.Create(
                request with
                {
                    Title = " Ready ",
                }));
    }
}