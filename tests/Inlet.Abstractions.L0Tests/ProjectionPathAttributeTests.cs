using System;


namespace Mississippi.Inlet.Abstractions.L0Tests;

/// <summary>
///     Tests projection path addressing and argument validation.
/// </summary>
public sealed class ProjectionPathAttributeTests
{
    /// <summary>
    ///     Empty or whitespace projection paths are rejected.
    /// </summary>
    /// <param name="path">The invalid path.</param>
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void ConstructorRejectsBlankPath(
        string path
    ) =>
        Assert.Throws<ArgumentException>(nameof(path), () => new ProjectionPathAttribute(path));

    /// <summary>
    ///     A null projection path is rejected.
    /// </summary>
    [Fact]
    public void ConstructorRejectsNullPath() =>
        Assert.Throws<ArgumentNullException>("path", () => new ProjectionPathAttribute(null!));

    /// <summary>
    ///     Entity paths preserve the supplied projection path and identifier.
    /// </summary>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="expected">The expected full path.</param>
    [Theory]
    [InlineData("account-1", "bank/accounts/account-1")]
    [InlineData("", "bank/accounts/")]
    public void GetEntityPathAppendsIdentifier(
        string entityId,
        string expected
    )
    {
        ProjectionPathAttribute attribute = new("bank/accounts");
        Assert.Equal("bank/accounts", attribute.Path);
        Assert.Equal(expected, attribute.GetEntityPath(entityId));
    }

    /// <summary>
    ///     Null entity identifiers are rejected by both addressing methods.
    /// </summary>
    [Fact]
    public void GetEntityPathsRejectNullIdentifier()
    {
        ProjectionPathAttribute attribute = new("bank/accounts");
        Assert.Throws<ArgumentNullException>("entityId", () => attribute.GetEntityPath(null!));
        Assert.Throws<ArgumentNullException>("entityId", () => attribute.GetVersionedEntityPath(null!, 1));
    }

    /// <summary>
    ///     Versioned addresses include the complete long version value.
    /// </summary>
    /// <param name="version">The requested version.</param>
    /// <param name="expected">The expected full path.</param>
    [Theory]
    [InlineData(0L, "bank/accounts/account-1/0")]
    [InlineData(42L, "bank/accounts/account-1/42")]
    [InlineData(long.MaxValue, "bank/accounts/account-1/9223372036854775807")]
    public void GetVersionedEntityPathAppendsVersion(
        long version,
        string expected
    ) =>
        Assert.Equal(
            expected,
            new ProjectionPathAttribute("bank/accounts").GetVersionedEntityPath("account-1", version));
}