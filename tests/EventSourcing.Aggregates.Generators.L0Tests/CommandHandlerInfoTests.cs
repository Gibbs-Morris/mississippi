using Allure.Xunit.Attributes;

using Mississippi.EventSourcing.Aggregates.Generators.Models;


namespace Mississippi.EventSourcing.Aggregates.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="CommandHandlerInfo" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Aggregates Generators")]
[AllureSubSuite("CommandHandlerInfo")]
public sealed class CommandHandlerInfoTests
{
    /// <summary>
    ///     Verifies that the model can be instantiated with default values.
    /// </summary>
    [Fact]
    [AllureFeature("Model Instantiation")]
    public void CanInstantiateWithDefaults()
    {
        // Act
        CommandHandlerInfo sut = new();

        // Assert
        Assert.NotNull(sut);
        Assert.Equal(string.Empty, sut.CommandFullTypeName);
        Assert.Equal(string.Empty, sut.CommandTypeName);
        Assert.Equal(string.Empty, sut.CommandNamespace);
        Assert.Equal(string.Empty, sut.AggregateFullTypeName);
    }

    /// <summary>
    ///     Verifies that properties can be set.
    /// </summary>
    [Fact]
    [AllureFeature("Model Properties")]
    public void CanSetProperties()
    {
        // Arrange
        CommandHandlerInfo sut = new();

        // Act
        sut.CommandFullTypeName = "Sample.Commands.CreateUser";
        sut.CommandTypeName = "CreateUser";
        sut.CommandNamespace = "Sample.Commands";
        sut.AggregateFullTypeName = "Sample.UserAggregate";

        // Assert
        Assert.Equal("Sample.Commands.CreateUser", sut.CommandFullTypeName);
        Assert.Equal("CreateUser", sut.CommandTypeName);
        Assert.Equal("Sample.Commands", sut.CommandNamespace);
        Assert.Equal("Sample.UserAggregate", sut.AggregateFullTypeName);
    }
}