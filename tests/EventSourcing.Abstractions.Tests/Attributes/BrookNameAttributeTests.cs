using Mississippi.EventSourcing.Abstractions.Attributes;


namespace Mississippi.EventSourcing.Abstractions.Tests.Attributes;

/// <summary>
///     Contains unit tests that verify the behavior of the <see cref="BrookNameAttribute" /> class.
/// </summary>
public class BrookNameAttributeTests
{
    /// <summary>
    ///     Confirms that component strings consisting solely of digits or prefixed by digits are still
    ///     treated as valid and correctly formatted.
    /// </summary>
    /// <param name="appName">An application component consisting of, or starting with, digits.</param>
    /// <param name="moduleName">A module component consisting of, or starting with, digits.</param>
    /// <param name="name">A stream name component consisting of, or starting with, digits.</param>
    [Theory]
    [InlineData("123", "456", "789")]
    [InlineData("1APP", "2MOD", "3STR")]
    public void ConstructorDigitsOnlyOrPrefixedStillValid(
        string appName,
        string moduleName,
        string name
    )
    {
        BrookNameAttribute sut = new(appName, moduleName, name);
        Assert.Equal($"{appName}.{moduleName}.{name}", sut.BrookName);
    }

    /// <summary>
    ///     Ensures that the constructor rejects application names that contain whitespace, diacritics or
    ///     other illegal characters.
    /// </summary>
    /// <param name="badName">An invalid application name.</param>
    [Theory]
    [InlineData("APP NAME")]
    [InlineData("APP\tNAME")]
    [InlineData("ÁPP")]
    [InlineData("APP_NAME")]
    public void ConstructorInternalWhitespaceOrIllegalCharsThrows(
        string badName
    )
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => new BrookNameAttribute(badName, "MODULE", "STREAM"));
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
            Assert.Throws<ArgumentException>(() => new BrookNameAttribute(appName, "MODULE", "STREAM"));
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
            Assert.Throws<ArgumentException>(() => new BrookNameAttribute("APP", moduleName, "STREAM"));
        Assert.Equal("moduleName", ex.ParamName);
    }

    /// <summary>
    ///     Ensures that the constructor throws an <see cref="ArgumentException" /> when the
    ///     <paramref name="name" /> argument does not match the required formatting rules.
    /// </summary>
    /// <param name="name">An invalid stream name.</param>
    [Theory]
    [InlineData("stream")]
    [InlineData("Stream")]
    [InlineData("STREAM123!")]
    public void ConstructorInvalidNameFormatThrowsArgumentException(
        string name
    )
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => new BrookNameAttribute("APP", "MODULE", name));
        Assert.Equal("name", ex.ParamName);
    }

    /// <summary>
    ///     Ensures that the constructor throws an <see cref="ArgumentException" /> when the
    ///     <paramref name="appName" /> argument is <see langword="null" />, empty or consists only of
    ///     whitespace characters.
    /// </summary>
    /// <param name="appName">The application name to validate.</param>
    /// <param name="moduleName">The module name supplied to the constructor (ignored for this test).</param>
    /// <param name="name">The stream name supplied to the constructor (ignored for this test).</param>
    [Theory]
    [InlineData(null, "MODULE", "STREAM")]
    [InlineData("", "MODULE", "STREAM")]
    [InlineData(" ", "MODULE", "STREAM")]
    public void ConstructorNullOrEmptyAppNameThrowsArgumentException(
        string? appName,
        string moduleName,
        string name
    )
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => new BrookNameAttribute(appName!, moduleName, name));
        Assert.Equal("appName", ex.ParamName);
    }

    /// <summary>
    ///     Ensures that the constructor throws an <see cref="ArgumentException" /> when the
    ///     <paramref name="moduleName" /> argument is <see langword="null" />, empty or consists only of
    ///     whitespace characters.
    /// </summary>
    /// <param name="appName">The application name supplied to the constructor (ignored for this test).</param>
    /// <param name="moduleName">The module name to validate.</param>
    /// <param name="name">The stream name supplied to the constructor (ignored for this test).</param>
    [Theory]
    [InlineData("APP", null, "STREAM")]
    [InlineData("APP", "", "STREAM")]
    [InlineData("APP", " ", "STREAM")]
    public void ConstructorNullOrEmptyModuleNameThrowsArgumentException(
        string appName,
        string? moduleName,
        string name
    )
    {
        ArgumentException ex =
            Assert.Throws<ArgumentException>(() => new BrookNameAttribute(appName, moduleName!, name));
        Assert.Equal("moduleName", ex.ParamName);
    }

    /// <summary>
    ///     Ensures that the constructor throws an <see cref="ArgumentException" /> when the
    ///     <paramref name="name" /> argument is <see langword="null" />, empty or consists only of
    ///     whitespace characters.
    /// </summary>
    /// <param name="appName">The application name supplied to the constructor (ignored for this test).</param>
    /// <param name="moduleName">The module name supplied to the constructor (ignored for this test).</param>
    /// <param name="name">The stream name to validate.</param>
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
            Assert.Throws<ArgumentException>(() => new BrookNameAttribute(appName, moduleName, name!));
        Assert.Equal("name", ex.ParamName);
    }

    /// <summary>
    ///     Verifies that alphanumeric values are accepted and correctly applied to the attribute
    ///     properties and the generated stream name.
    /// </summary>
    /// <param name="appName">The application name to test.</param>
    /// <param name="moduleName">The module name to test.</param>
    /// <param name="name">The stream name to test.</param>
    [Theory]
    [InlineData("APP123", "MODULE456", "STREAM789")]
    [InlineData("A", "M", "S")]
    [InlineData("1APP", "2MOD", "3STR")]
    public void ConstructorValidAlphanumericParametersSetsPropertiesCorrectly(
        string appName,
        string moduleName,
        string name
    )
    {
        BrookNameAttribute sut = new(appName, moduleName, name);
        Assert.Equal(appName, sut.AppName);
        Assert.Equal(moduleName, sut.ModuleName);
        Assert.Equal(name, sut.Name);
        Assert.Equal($"{appName}.{moduleName}.{name}", sut.BrookName);
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
        const string name = "STREAM";
        BrookNameAttribute sut = new(appName, moduleName, name);
        Assert.Equal(appName, sut.AppName);
        Assert.Equal(moduleName, sut.ModuleName);
        Assert.Equal(name, sut.Name);
        Assert.Equal("APP.MODULE.STREAM", sut.BrookName);
    }
}