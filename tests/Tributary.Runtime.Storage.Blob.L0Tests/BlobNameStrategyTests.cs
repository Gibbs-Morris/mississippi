using Microsoft.Extensions.Options;

using Mississippi.Tributary.Abstractions;
using Mississippi.Tributary.Runtime.Storage.Blob.Naming;


namespace Mississippi.Tributary.Runtime.Storage.Blob.L0Tests;

/// <summary>
///     Verifies increment-2 Blob naming semantics.
/// </summary>
public sealed class BlobNameStrategyTests
{
    /// <summary>
    ///     Verifies canonical stream identity matches the persisted increment-2 JSON contract.
    /// </summary>
    [Fact]
    public void GetCanonicalStreamIdentityShouldMatchGoldenJsonContract()
    {
        SnapshotStreamKey streamKey = new("brook-a", "projection-a", "entity-42", "reducers-v1");
        BlobNameStrategy strategy = CreateStrategy();

        string canonicalIdentity = strategy.GetCanonicalStreamIdentity(streamKey);

        Assert.Equal(
            "{\"brookName\":\"brook-a\",\"snapshotStorageName\":\"projection-a\",\"entityId\":\"entity-42\",\"reducersHash\":\"reducers-v1\"}",
            canonicalIdentity);
        Assert.NotEqual(streamKey.ToString(), canonicalIdentity);
    }

    /// <summary>
    ///     Verifies Blob prefix normalization and stream hashing produce the expected persisted prefix.
    /// </summary>
    [Fact]
    public void GetStreamPrefixShouldMatchGoldenHashContract()
    {
        SnapshotStreamKey streamKey = new("brook-a", "projection-a", "entity-42", "reducers-v1");
        BlobNameStrategy strategy = CreateStrategy(new SnapshotBlobStorageOptions
        {
            BlobPrefix = "/custom/root//",
        });

        string prefix = strategy.GetStreamPrefix(streamKey);

        Assert.Equal(
            "custom/root/472D9A7C673DED60A7CEBC820AE5D5BA219EDF04EABA0F71307330C3625B75C2/",
            prefix);
    }

    /// <summary>
    ///     Verifies versioned Blob names use the exact persisted increment-2 naming contract.
    /// </summary>
    [Fact]
    public void GetBlobNameShouldMatchGoldenBlobNameContract()
    {
        SnapshotKey snapshotKey = new(new("brook-a", "projection-a", "entity-42", "reducers-v1"), 10);
        BlobNameStrategy strategy = CreateStrategy();

        string blobName = strategy.GetBlobName(snapshotKey);

        Assert.Equal(
            "snapshots/472D9A7C673DED60A7CEBC820AE5D5BA219EDF04EABA0F71307330C3625B75C2/v00000000000000000010.snapshot",
            blobName);
    }

    /// <summary>
    ///     Verifies lexically sorted Blob names preserve version order across numeric boundaries.
    /// </summary>
    /// <param name="version">The snapshot version.</param>
    /// <param name="expectedSuffix">The expected zero-padded Blob name suffix.</param>
    [Theory]
    [InlineData(9, "v00000000000000000009.snapshot")]
    [InlineData(10, "v00000000000000000010.snapshot")]
    [InlineData(11, "v00000000000000000011.snapshot")]
    [InlineData(100, "v00000000000000000100.snapshot")]
    public void BlobNamesShouldSortLexicallyByVersion(
        long version,
        string expectedSuffix
    )
    {
        BlobNameStrategy strategy = CreateStrategy();
        SnapshotStreamKey streamKey = new("brook-a", "projection-a", "entity-42", "reducers-v1");

        string blobName = strategy.GetBlobName(new(streamKey, version));

        Assert.EndsWith(expectedSuffix, blobName, System.StringComparison.Ordinal);
    }

    /// <summary>
    ///     Verifies version parsing succeeds for a matching stream-local Blob name.
    /// </summary>
    [Fact]
    public void TryParseVersionShouldParseMatchingBlobName()
    {
        SnapshotStreamKey streamKey = new("brook-a", "projection-a", "entity-42", "reducers-v1");
        BlobNameStrategy strategy = CreateStrategy();
        string blobName = strategy.GetBlobName(new(streamKey, 321));

        bool parsed = strategy.TryParseVersion(blobName, streamKey, out long version);

        Assert.True(parsed);
        Assert.Equal(321, version);
    }

    /// <summary>
    ///     Verifies version parsing rejects Blob names belonging to a different stream.
    /// </summary>
    [Fact]
    public void TryParseVersionShouldRejectBlobNameForDifferentStream()
    {
        SnapshotStreamKey expectedStream = new("brook-a", "projection-a", "entity-42", "reducers-v1");
        SnapshotStreamKey otherStream = new("brook-a", "projection-a", "entity-43", "reducers-v1");
        BlobNameStrategy strategy = CreateStrategy();
        string blobName = strategy.GetBlobName(new(otherStream, 321));

        bool parsed = strategy.TryParseVersion(blobName, expectedStream, out long version);

        Assert.False(parsed);
        Assert.Equal(0, version);
    }

    private static BlobNameStrategy CreateStrategy(
        SnapshotBlobStorageOptions? options = null
    ) =>
        new(Options.Create(options ?? new SnapshotBlobStorageOptions()));
}