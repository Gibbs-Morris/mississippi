using System;

using Allure.Xunit.Attributes;

using EventStorageNameAttribute = Mississippi.EventSourcing.Brooks.Abstractions.Attributes.EventStorageNameAttribute;


namespace Mississippi.EventSourcing.Abstractions.Tests.Attributes;

/// <summary>
///     Contains unit tests that verify the behaviour of the <see cref="EventStorageNameAttribute" /> class.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Brooks Abstractions")]
[AllureSubSuite("Event Storage Name Attribute")]
public sealed class EventStorageNameAttributeTests
{
    /// <summary>
    ///     Confirms that component strings consisting solely of digits or prefixed by digits are still
    ///     treated as valid and correctly formatted.
    /// </summary>
    /// <param name="app">An application component consisting of, or starting with, digits.</param>
    /// <param name="mod">A module component consisting of, or starting with, digits.</param>
    /// <param name="evt">An event component consisting of, or starting with, digits.</param>
    /// <param name="version">The version number.</param>
    [Theory]
    [InlineData("123", "456", "789", 42)]
    [InlineData("1APP", "2MOD", "3EVT", 1)]
    public void ConstructorDigitsOnlyOrPrefixedStillValid(
        string app,
        string mod,
        string evt,
        int version
    )
    {
        EventStorageNameAttribute sut = new(app, mod, evt, version);
        Assert.Equal($"{app}.{mod}.{evt}.V{version}", sut.StorageName);
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
            Assert.Throws<ArgumentException>(() => new EventStorageNameAttribute(badApp, "MODULE", "EVENT"));
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
            Assert.Throws<ArgumentException>(() => new EventStorageNameAttribute(appName, "MODULE", "EVENT"));
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
            Assert.Throws<ArgumentException>(() => new EventStorageNameAttribute("APP", moduleName, "EVENT"));
        Assert.Equal("moduleName", ex.ParamName);
    }

    /// <summary>
    ///     Ensures that the constructor throws an <see cref="ArgumentException" /> when the
    ///     <paramref name="name" /> argument does not match the required formatting rules.
    /// </summary>
    /// <param name="name">An invalid event name.</param>
    [Theory]
    [InlineData("event")]
    [InlineData("Event")]
    [InlineData("EVENT123!")]
    public void ConstructorInvalidNameFormatThrowsArgumentException(
        string name
    )
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => new EventStorageNameAttribute("APP", "MODULE", name));
        Assert.Equal("name", ex.ParamName);
    }

    /// <summary>
    ///     Ensures that the constructor throws an <see cref="ArgumentException" /> when the supplied
    ///     version is less than or equal to zero.
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
            Assert.Throws<ArgumentException>(() => new EventStorageNameAttribute("APP", "MODULE", "EVENT", version));
        Assert.Equal("version", ex.ParamName);
    }

    /// <summary>
    ///     Ensures that the constructor throws an <see cref="ArgumentException" /> when the
    ///     <paramref name="appName" /> argument is <see langword="null" />, empty or consists only of
    ///     whitespace characters.
    /// </summary>
    /// <param name="appName">The application name to validate.</param>
    /// <param name="moduleName">The module name supplied to the constructor (ignored for this test).</param>
    /// <param name="name">The event name supplied to the constructor (ignored for this test).</param>
    [Theory]
    [InlineData(null, "", "EVENT")]
    [InlineData("", "MODULE", "EVENT")]
    [InlineData(" ", "MODULE", "EVENT")]
    public void ConstructorNullOrEmptyAppNameThrowsArgumentException(
        string? appName,
        string moduleName,
        string name
    )
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => new EventStorageNameAttribute(appName!, moduleName, name));
        Assert.Equal("appName", ex.ParamName);
    }

    /// <summary>
    ///     Ensures that the constructor throws an <see cref="ArgumentException" /> when the
    ///     <paramref name="moduleName" /> argument is <see langword="null" />, empty or consists only of
    ///     whitespace characters.
    /// </summary>
    /// <param name="appName">The application name supplied to the constructor (ignored for this test).</param>
    /// <param name="moduleName">The module name to validate.</param>
    /// <param name="name">The event name supplied to the constructor (ignored for this test).</param>
    [Theory]
    [InlineData("APP", null, "EVENT")]
    [InlineData("APP", "", "EVENT")]
    [InlineData("APP", " ", "EVENT")]
    public void ConstructorNullOrEmptyModuleNameThrowsArgumentException(
        string appName,
        string? moduleName,
        string name
    )
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => new EventStorageNameAttribute(appName, moduleName!, name));
        Assert.Equal("moduleName", ex.ParamName);
    }

    /// <summary>
    ///     Ensures that the constructor throws an <see cref="ArgumentException" /> when the
    ///     <paramref name="name" /> argument is <see langword="null" />, empty or consists only of
    ///     whitespace characters.
    /// </summary>
    /// <param name="appName">The application name supplied to the constructor (ignored for this test).</param>
    /// <param name="moduleName">The module name supplied to the constructor (ignored for this test).</param>
    /// <param name="name">The event name to validate.</param>
    [Theory]
    [InlineData("APP", "MODULE", null)]
    [InlineData("APP", "MODULE", "")]
    [InlineData("APP", "MODULE", " ")]
    public void ConstructorNullOrEmptyNameThrowsArgumentException(
        string appName,
        string moduleName,
        string? name
    )
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => new EventStorageNameAttribute(appName, moduleName, name!));
        Assert.Equal("name", ex.ParamName);
    }

    /// <summary>
    ///     Ensures that the optional <c>version</c> constructor parameter defaults to <c>1</c> when
    ///     omitted.
    /// </summary>
    [Fact]
    public void ConstructorOmittingOptionalVersionDefaultsToOne()
    {
        EventStorageNameAttribute sut = new("APP", "MODULE", "EVENT");
        Assert.Equal(1, sut.Version);
        Assert.Equal("APP.MODULE.EVENT.V1", sut.StorageName);
    }

    /// <summary>
    ///     Verifies that alphanumeric values are accepted and correctly applied to the attribute
    ///     properties and the generated storage name.
    /// </summary>
    /// <param name="appName">The application name to test.</param>
    /// <param name="moduleName">The module name to test.</param>
    /// <param name="name">The event name to test.</param>
    /// <param name="version">The version number to test.</param>
    [Theory]
    [InlineData("APP123", "MODULE456", "EVENT789", 1)]
    [InlineData("A", "M", "E", 1)]
    [InlineData("A0Z9", "B1Y8", "C2X7", 999)]
    public void ConstructorValidAlphanumericParametersSetsPropertiesCorrectly(
        string appName,
        string moduleName,
        string name,
        int version
    )
    {
        EventStorageNameAttribute sut = new(appName, moduleName, name, version);
        Assert.Equal(appName, sut.AppName);
        Assert.Equal(moduleName, sut.ModuleName);
        Assert.Equal(name, sut.Name);
        Assert.Equal(version, sut.Version);
        Assert.Equal($"{appName}.{moduleName}.{name}.V{version}", sut.StorageName);
    }

    /// <summary>
    ///     Verifies that the constructor assigns the supplied parameters to the corresponding
    ///     properties when all arguments are valid.
    /// </summary>
    [Fact]
    public void ConstructorValidParametersSetsPropertiesCorrectly()
    {
        const string appName = "APP";
        const string moduleName = "MODULE";
        const string name = "EVENT";
        const int version = 3;
        EventStorageNameAttribute sut = new(appName, moduleName, name, version);
        Assert.Equal(appName, sut.AppName);
        Assert.Equal(moduleName, sut.ModuleName);
        Assert.Equal(name, sut.Name);
        Assert.Equal(version, sut.Version);
        Assert.Equal("APP.MODULE.EVENT.V3", sut.StorageName);
    }

    /// <summary>
    ///     Verifies that valid positive integer versions are accepted and reflected in the attribute's
    ///     <see cref="EventStorageNameAttribute.Version" /> property and the formatted
    ///     <see cref="EventStorageNameAttribute.StorageName" />.
    /// </summary>
    /// <param name="version">A valid positive version number.</param>
    [Theory]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void ConstructorValidVersionSetsPropertyCorrectly(
        int version
    )
    {
        EventStorageNameAttribute sut = new("APP", "MODULE", "EVENT", version);
        Assert.Equal(version, sut.Version);
        Assert.Equal($"APP.MODULE.EVENT.V{version}", sut.StorageName);
    }

    /// <summary>
    ///     Verifies that <see cref="EventStorageNameAttribute.StorageName" /> returns the expected value based on
    ///     valid constructor arguments.
    /// </summary>
    /// <param name="appName">The application component of the event name.</param>
    /// <param name="moduleName">The module component of the event name.</param>
    /// <param name="name">The event name component.</param>
    /// <param name="version">The version component.</param>
    /// <param name="expected">The expected fully‑qualified storage name.</param>
    [Theory]
    [InlineData("APP", "MODULE", "EVENT", 1, "APP.MODULE.EVENT.V1")]
    [InlineData("APP", "MODULE", "EVENT", 2, "APP.MODULE.EVENT.V2")]
    [InlineData("TEST", "USER", "CREATED", 1, "TEST.USER.CREATED.V1")]
    public void StorageNameReturnsCorrectFormat(
        string appName,
        string moduleName,
        string name,
        int version,
        string expected
    )
    {
        EventStorageNameAttribute sut = new(appName, moduleName, name, version);
        Assert.Equal(expected, sut.StorageName);
    }
}