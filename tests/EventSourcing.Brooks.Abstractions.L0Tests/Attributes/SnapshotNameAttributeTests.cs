using System;

using Allure.Xunit.Attributes;

using SnapshotNameAttribute = Mississippi.EventSourcing.Brooks.Abstractions.Attributes.SnapshotNameAttribute;


namespace Mississippi.EventSourcing.Abstractions.Tests.Attributes;

/// <summary>
///     Contains unit tests that verify the behaviour of the <see cref="SnapshotNameAttribute" /> class.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Brooks Abstractions")]
[AllureSubSuite("Snapshot Name Attribute")]
public sealed class SnapshotNameAttributeTests
{
    /// <summary>
    ///     Verifies that the default version is 1 when not explicitly specified.
    /// </summary>
    [Fact]
    public void ConstructorDefaultsVersionToOne()
    {
        SnapshotNameAttribute sut = new("APP", "MODULE", "STATE");
        Assert.Equal(1, sut.Version);
    }

    /// <summary>
    ///     Confirms that component strings consisting solely of digits or prefixed by digits are still
    ///     treated as valid and correctly formatted.
    /// </summary>
    /// <param name="app">An application component consisting of, or starting with, digits.</param>
    /// <param name="mod">A module component consisting of, or starting with, digits.</param>
    /// <param name="snapshot">A snapshot component consisting of, or starting with, digits.</param>
    /// <param name="version">The version number.</param>
    [Theory]
    [InlineData("123", "456", "789", 42)]
    [InlineData("1APP", "2MOD", "3STATE", 1)]
    public void ConstructorDigitsOnlyOrPrefixedStillValid(
        string app,
        string mod,
        string snapshot,
        int version
    )
    {
        SnapshotNameAttribute sut = new(app, mod, snapshot, version);
        Assert.Equal($"{app}.{mod}.{snapshot}.V{version}", sut.SnapshotName);
    }

    /// <summary>
    ///     Ensures that the constructor rejects application names that contain whitespace, diacritics or
    ///     other illegal characters.
    /// </summary>
    /// <param name="badApp">An invalid application name.</param>
    [Theory]
    [InlineData("APP NAME")]
    [InlineData("APP\tNAME")]
    [InlineData("ÁPP")]
    [InlineData("APP_NAME")]
    public void ConstructorInternalWhitespaceOrIllegalCharsThrows(
        string badApp
    )
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => new SnapshotNameAttribute(badApp, "MODULE", "STATE"));
        Assert.Equal("appName", ex.ParamName);
    }

    /// <summary>
    ///     Ensures that the constructor throws an <see cref="ArgumentException" /> when the
    ///     <paramref name="appName" /> argument does not match the required formatting rules.
    /// </summary>
    /// <param name="appName">An invalid application name.</param>
    [Theory]
    [InlineData("app")]
    [InlineData("App")]
    [InlineData("APP123!")]
    public void ConstructorInvalidAppNameFormatThrowsArgumentException(
        string appName
    )
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => new SnapshotNameAttribute(appName, "MODULE", "STATE"));
        Assert.Equal("appName", ex.ParamName);
    }

    /// <summary>
    ///     Ensures that the constructor throws an <see cref="ArgumentException" /> when the
    ///     <paramref name="moduleName" /> argument does not match the required formatting rules.
    /// </summary>
    /// <param name="moduleName">An invalid module name.</param>
    [Theory]
    [InlineData("module")]
    [InlineData("Module")]
    [InlineData("MODULE123!")]
    public void ConstructorInvalidModuleNameFormatThrowsArgumentException(
        string moduleName
    )
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => new SnapshotNameAttribute("APP", moduleName, "STATE"));
        Assert.Equal("moduleName", ex.ParamName);
    }

    /// <summary>
    ///     Ensures that the constructor throws an <see cref="ArgumentException" /> when the
    ///     <paramref name="name" /> argument does not match the required formatting rules.
    /// </summary>
    /// <param name="name">An invalid snapshot name.</param>
    [Theory]
    [InlineData("state")]
    [InlineData("State")]
    [InlineData("STATE123!")]
    public void ConstructorInvalidNameFormatThrowsArgumentException(
        string name
    )
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => new SnapshotNameAttribute("APP", "MODULE", name));
        Assert.Equal("name", ex.ParamName);
    }

    /// <summary>
    ///     Ensures that the constructor throws an <see cref="ArgumentException" /> when the
    ///     <paramref name="version" /> argument is zero or negative.
    /// </summary>
    /// <param name="version">An invalid version number.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ConstructorInvalidVersionThrowsArgumentException(
        int version
    )
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => new SnapshotNameAttribute("APP", "MODULE", "STATE", version));
        Assert.Equal("version", ex.ParamName);
    }

    /// <summary>
    ///     Ensures that the constructor throws an <see cref="ArgumentException" /> when the
    ///     <paramref name="appName" /> argument is null or whitespace.
    /// </summary>
    /// <param name="appName">The invalid app name.</param>
    /// <param name="moduleName">The module name.</param>
    /// <param name="name">The snapshot name.</param>
    [Theory]
    [InlineData(null, "MODULE", "STATE")]
    [InlineData("", "MODULE", "STATE")]
    [InlineData("   ", "MODULE", "STATE")]
    public void ConstructorNullOrWhitespaceAppNameThrowsArgumentException(
        string? appName,
        string moduleName,
        string name
    )
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => new SnapshotNameAttribute(appName!, moduleName, name));
        Assert.Equal("appName", ex.ParamName);
    }

    /// <summary>
    ///     Ensures that the constructor throws an <see cref="ArgumentException" /> when the
    ///     <paramref name="moduleName" /> argument is null or whitespace.
    /// </summary>
    /// <param name="appName">The app name.</param>
    /// <param name="moduleName">The invalid module name.</param>
    /// <param name="name">The snapshot name.</param>
    [Theory]
    [InlineData("APP", null, "STATE")]
    [InlineData("APP", "", "STATE")]
    [InlineData("APP", "   ", "STATE")]
    public void ConstructorNullOrWhitespaceModuleNameThrowsArgumentException(
        string appName,
        string? moduleName,
        string name
    )
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => new SnapshotNameAttribute(appName, moduleName!, name));
        Assert.Equal("moduleName", ex.ParamName);
    }

    /// <summary>
    ///     Ensures that the constructor throws an <see cref="ArgumentException" /> when the
    ///     <paramref name="name" /> argument is null or whitespace.
    /// </summary>
    /// <param name="appName">The app name.</param>
    /// <param name="moduleName">The module name.</param>
    /// <param name="name">The invalid snapshot name.</param>
    [Theory]
    [InlineData("APP", "MODULE", null)]
    [InlineData("APP", "MODULE", "")]
    [InlineData("APP", "MODULE", "   ")]
    public void ConstructorNullOrWhitespaceNameThrowsArgumentException(
        string appName,
        string moduleName,
        string? name
    )
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => new SnapshotNameAttribute(appName, moduleName, name!));
        Assert.Equal("name", ex.ParamName);
    }

    /// <summary>
    ///     Verifies that valid constructor arguments produce the expected property values.
    /// </summary>
    /// <param name="appName">The application name component.</param>
    /// <param name="moduleName">The module name component.</param>
    /// <param name="name">The snapshot name component.</param>
    /// <param name="version">The version number.</param>
    [Theory]
    [InlineData("APP", "MODULE", "STATE", 1)]
    [InlineData("MYAPP", "MYMODULE", "MYSTATE", 5)]
    public void ConstructorSetsProperties(
        string appName,
        string moduleName,
        string name,
        int version
    )
    {
        SnapshotNameAttribute sut = new(appName, moduleName, name, version);
        Assert.Equal(appName, sut.AppName);
        Assert.Equal(moduleName, sut.ModuleName);
        Assert.Equal(name, sut.Name);
        Assert.Equal(version, sut.Version);
    }

    /// <summary>
    ///     Verifies that valid constructor arguments produce the expected snapshot name format.
    /// </summary>
    /// <param name="appName">The application name component.</param>
    /// <param name="moduleName">The module name component.</param>
    /// <param name="name">The snapshot name component.</param>
    /// <param name="version">The version number.</param>
    [Theory]
    [InlineData("APP", "MODULE", "STATE", 1)]
    [InlineData("MYAPP", "MYMODULE", "MYSTATE", 5)]
    public void ConstructorSetsSnapshotNameProperty(
        string appName,
        string moduleName,
        string name,
        int version
    )
    {
        SnapshotNameAttribute sut = new(appName, moduleName, name, version);
        Assert.Equal($"{appName}.{moduleName}.{name}.V{version}", sut.SnapshotName);
    }

    /// <summary>
    ///     Verifies that <see cref="SnapshotNameAttribute.SnapshotName" /> returns the expected value based on
    ///     the constructor arguments.
    /// </summary>
    /// <param name="appName">The application name component.</param>
    /// <param name="moduleName">The module name component.</param>
    /// <param name="name">The snapshot name component.</param>
    /// <param name="version">The version number.</param>
    /// <param name="expected">The expected snapshot name string.</param>
    [Theory]
    [InlineData("APP", "MODULE", "STATE", 1, "APP.MODULE.STATE.V1")]
    [InlineData("MYAPP", "MYMODULE", "MYSTATE", 2, "MYAPP.MYMODULE.MYSTATE.V2")]
    [InlineData("A", "B", "C", 999, "A.B.C.V999")]
    public void SnapshotNamePropertyReturnsFormattedString(
        string appName,
        string moduleName,
        string name,
        int version,
        string expected
    )
    {
        SnapshotNameAttribute sut = new(appName, moduleName, name, version);
        Assert.Equal(expected, sut.SnapshotName);
    }

    /// <summary>
    ///     Verifies that <see cref="SnapshotNameAttribute.Version" /> property and the formatted
    ///     <see cref="SnapshotNameAttribute.SnapshotName" /> correctly reflect positive integer versions.
    /// </summary>
    /// <param name="version">The version number to test.</param>
    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    [InlineData(1000)]
    public void VersionPropertyReflectsPositiveVersions(
        int version
    )
    {
        SnapshotNameAttribute sut = new("APP", "MODULE", "STATE", version);
        Assert.Equal(version, sut.Version);
        Assert.EndsWith($".V{version}", sut.SnapshotName, StringComparison.Ordinal);
    }
}