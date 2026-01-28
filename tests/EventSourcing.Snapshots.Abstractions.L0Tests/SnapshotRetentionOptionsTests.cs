using System;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Snapshots.Abstractions.L0Tests;

/// <summary>
///     Tests for <see cref="SnapshotRetentionOptions" /> to verify base snapshot version calculation.
/// </summary>
public sealed class SnapshotRetentionOptionsTests
{
    /// <summary>
    ///     Test snapshot type with a storage name attribute.
    /// </summary>
    [SnapshotStorageName("TESTAPP", "TESTMODULE", "TESTSNAP")]
    private sealed record AttributedSnapshot;

    /// <summary>
    ///     Test snapshot type used for retention options testing.
    /// </summary>
    /// <param name="Value">Gets a placeholder value for the test snapshot.</param>
    private sealed record TestSnapshot(int Value = 0);

    /// <summary>
    ///     Verifies that the generic overload delegates correctly.
    /// </summary>
    [Fact]
    public void GetBaseSnapshotVersionGenericDelegatesToNonGeneric()
    {
        SnapshotRetentionOptions options = new()
        {
            DefaultRetainModulus = 5,
        };
        long result = options.GetBaseSnapshotVersion<TestSnapshot>(7);
        Assert.Equal(5, result);
    }

    /// <summary>
    ///     Verifies that state type overrides are respected.
    /// </summary>
    [Fact]
    public void GetBaseSnapshotVersionRespectsTypeOverride()
    {
        SnapshotRetentionOptions options = new()
        {
            DefaultRetainModulus = 100,
        };
        options.StateTypeOverrides[typeof(TestSnapshot).FullName!] = 10;
        long result = options.GetBaseSnapshotVersion<TestSnapshot>(25);
        Assert.Equal(20, result);
    }

    /// <summary>
    ///     Verifies that base version is calculated correctly for non-boundary cases.
    /// </summary>
    /// <param name="targetVersion">The target version to calculate the base for.</param>
    /// <param name="modulus">The retention modulus interval.</param>
    /// <param name="expectedBase">The expected base version result.</param>
    [Theory]
    [InlineData(1, 5, 0)]
    [InlineData(4, 5, 0)]
    [InlineData(6, 5, 5)]
    [InlineData(9, 5, 5)]
    [InlineData(11, 5, 10)]
    [InlineData(99, 100, 0)]
    [InlineData(101, 100, 100)]
    [InlineData(199, 100, 100)]
    [InlineData(364, 100, 300)]
    public void GetBaseSnapshotVersionReturnsCorrectBaseForNonBoundary(
        long targetVersion,
        int modulus,
        long expectedBase
    )
    {
        SnapshotRetentionOptions options = new()
        {
            DefaultRetainModulus = modulus,
        };
        long result = options.GetBaseSnapshotVersion<TestSnapshot>(targetVersion);
        Assert.Equal(expectedBase, result);
    }

    /// <summary>
    ///     Verifies that base version is strictly less than target when target equals modulus.
    ///     This prevents self-referential grain calls that cause deadlocks.
    /// </summary>
    /// <param name="targetVersion">The target version to calculate the base for.</param>
    /// <param name="modulus">The retention modulus interval.</param>
    /// <param name="expectedBase">The expected base version result.</param>
    [Theory]
    [InlineData(5, 5, 0)]
    [InlineData(10, 5, 5)]
    [InlineData(100, 100, 0)]
    [InlineData(200, 100, 100)]
    public void GetBaseSnapshotVersionReturnsStrictlyLessThanTargetAtBoundary(
        long targetVersion,
        int modulus,
        long expectedBase
    )
    {
        SnapshotRetentionOptions options = new()
        {
            DefaultRetainModulus = modulus,
        };
        long result = options.GetBaseSnapshotVersion<TestSnapshot>(targetVersion);
        Assert.Equal(expectedBase, result);
        Assert.True(result < targetVersion, $"Base {result} should be strictly less than target {targetVersion}");
    }

    /// <summary>
    ///     Verifies that zero or negative target versions return zero.
    /// </summary>
    /// <param name="targetVersion">The target version to test.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void GetBaseSnapshotVersionReturnsZeroForZeroOrNegativeTarget(
        long targetVersion
    )
    {
        SnapshotRetentionOptions options = new()
        {
            DefaultRetainModulus = 100,
        };
        long result = options.GetBaseSnapshotVersion<TestSnapshot>(targetVersion);
        Assert.Equal(0, result);
    }

    /// <summary>
    ///     Verifies boundary behavior with small modulus to confirm no self-reference.
    /// </summary>
    [Fact]
    public void GetBaseSnapshotVersionWithModulusOneNeverReturnsSameAsTarget()
    {
        SnapshotRetentionOptions options = new()
        {
            DefaultRetainModulus = 1,
        };
        for (long target = 1; target <= 10; target++)
        {
            long result = options.GetBaseSnapshotVersion<TestSnapshot>(target);
            Assert.True(result < target, $"Base {result} should be strictly less than target {target}");
        }
    }

    /// <summary>
    ///     Verifies that the generic GetRetainModulus delegates to the non-generic overload.
    /// </summary>
    [Fact]
    public void GetRetainModulusGenericReturnsDefaultWhenNoOverride()
    {
        SnapshotRetentionOptions options = new()
        {
            DefaultRetainModulus = 42,
        };
        int result = options.GetRetainModulus<TestSnapshot>();
        Assert.Equal(42, result);
    }

    /// <summary>
    ///     Verifies that GetRetainModulus prefers snapshot storage name over CLR type name.
    /// </summary>
    [Fact]
    public void GetRetainModulusPrefersSnapshotStorageNameOverClrTypeName()
    {
        SnapshotRetentionOptions options = new()
        {
            DefaultRetainModulus = 100,
        };

        // Set override using storage name (format: AppName.ModuleName.Name.V{Version})
        options.StateTypeOverrides["TESTAPP.TESTMODULE.TESTSNAP.V1"] = 50;

        // Set override using CLR name (should be ignored when storage name matches)
        options.StateTypeOverrides[typeof(AttributedSnapshot).FullName!] = 75;
        int result = options.GetRetainModulus<AttributedSnapshot>();
        Assert.Equal(50, result);
    }

    /// <summary>
    ///     Verifies that GetRetainModulus respects type override using CLR type name.
    /// </summary>
    [Fact]
    public void GetRetainModulusRespectsClrTypeNameOverride()
    {
        SnapshotRetentionOptions options = new()
        {
            DefaultRetainModulus = 100,
        };
        options.StateTypeOverrides[typeof(TestSnapshot).FullName!] = 25;
        int result = options.GetRetainModulus<TestSnapshot>();
        Assert.Equal(25, result);
    }

    /// <summary>
    ///     Verifies that GetRetainModulus throws for null type.
    /// </summary>
    [Fact]
    public void GetRetainModulusThrowsForNullType()
    {
        SnapshotRetentionOptions options = new();
        Assert.Throws<ArgumentNullException>(() => options.GetRetainModulus(null!));
    }
}