using System;

using Allure.Xunit.Attributes;

using Mississippi.Common.Abstractions;

using NSubstitute;

using Orleans.Hosting;


namespace Mississippi.Aqueduct.Grains.L0Tests;

/// <summary>
///     Tests for <see cref="AqueductSiloOptions" />.
/// </summary>
/// <remarks>
///     Note: Tests for <see cref="AqueductSiloOptions.UseMemoryStreams()" /> require
///     real Orleans silo infrastructure and are covered in L2 integration tests.
/// </remarks>
[AllureParentSuite("Aqueduct")]
[AllureSuite("Options")]
[AllureSubSuite("AqueductSiloOptions")]
public sealed class AqueductSiloOptionsTests
{
    /// <summary>
    ///     Constructor should throw when siloBuilder is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void ConstructorThrowsWhenSiloBuilderIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new AqueductSiloOptions(null!));
    }

    /// <summary>
    ///     Default property values should be set correctly.
    /// </summary>
    [Fact]
    [AllureFeature("Default Values")]
    public void DefaultPropertyValuesAreSetCorrectly()
    {
        // Arrange
        ISiloBuilder siloBuilder = Substitute.For<ISiloBuilder>();

        // Act
        AqueductSiloOptions options = new(siloBuilder);

        // Assert
        Assert.Equal(MississippiDefaults.StreamProviderName, options.StreamProviderName);
        Assert.Equal(MississippiDefaults.StreamNamespaces.AllClients, options.AllClientsStreamNamespace);
        Assert.Equal(MississippiDefaults.StreamNamespaces.Server, options.ServerStreamNamespace);
        Assert.Equal(1, options.HeartbeatIntervalMinutes);
        Assert.Equal(3, options.DeadServerTimeoutMultiplier);
    }

    /// <summary>
    ///     Properties should be settable.
    /// </summary>
    [Fact]
    [AllureFeature("Configuration")]
    public void PropertiesAreSettable()
    {
        // Arrange
        ISiloBuilder siloBuilder = Substitute.For<ISiloBuilder>();
        AqueductSiloOptions options = new(siloBuilder);

        // Act
        options.StreamProviderName = "CustomStream";
        options.AllClientsStreamNamespace = "CustomAllClients";
        options.ServerStreamNamespace = "CustomServer";
        options.HeartbeatIntervalMinutes = 5;
        options.DeadServerTimeoutMultiplier = 10;

        // Assert
        Assert.Equal("CustomStream", options.StreamProviderName);
        Assert.Equal("CustomAllClients", options.AllClientsStreamNamespace);
        Assert.Equal("CustomServer", options.ServerStreamNamespace);
        Assert.Equal(5, options.HeartbeatIntervalMinutes);
        Assert.Equal(10, options.DeadServerTimeoutMultiplier);
    }
}