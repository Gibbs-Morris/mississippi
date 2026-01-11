using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Aggregates.Generators.Models;


namespace Mississippi.EventSourcing.Aggregates.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="CommandInfo" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Aggregates Generators")]
[AllureSubSuite("CommandInfo")]
public sealed class CommandInfoTests
{
    /// <summary>
    ///     Verifies that the model can be instantiated with default values.
    /// </summary>
    [Fact]
    [AllureFeature("Model Instantiation")]
    public void CanInstantiateWithDefaults()
    {
        // Act
        CommandInfo sut = new();

        // Assert
        Assert.NotNull(sut);
        Assert.Equal(string.Empty, sut.FullTypeName);
        Assert.Equal(string.Empty, sut.TypeName);
        Assert.Equal(string.Empty, sut.Namespace);
        Assert.Equal(string.Empty, sut.MethodName);
    }

    /// <summary>
    ///     Verifies that properties can be set.
    /// </summary>
    [Fact]
    [AllureFeature("Model Properties")]
    public void CanSetProperties()
    {
        // Arrange
        CommandInfo sut = new();

        // Act
        sut.FullTypeName = "Sample.Commands.CreateUser";
        sut.TypeName = "CreateUser";
        sut.Namespace = "Sample.Commands";
        sut.MethodName = "CreateUser";

        // Assert
        Assert.Equal("Sample.Commands.CreateUser", sut.FullTypeName);
        Assert.Equal("CreateUser", sut.TypeName);
        Assert.Equal("Sample.Commands", sut.Namespace);
        Assert.Equal("CreateUser", sut.MethodName);
    }
}