using System;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Abstractions.Attributes;
using Mississippi.EventSourcing.Aggregates.Abstractions;

using Moq;

using Orleans;


namespace Mississippi.EventSourcing.Aggregates.Tests;

/// <summary>
///     Tests for <see cref="AggregateGrainFactory" />.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Aggregates")]
[AllureSubSuite("Grain Factory")]
public class AggregateGrainFactoryTests
{
    /// <summary>
    ///     Test brook definition.
    /// </summary>
    [BrookName("TEST", "APP", "BROOK")]
    private sealed class TestBrook : IBrookDefinition
    {
        /// <summary>
        ///     Gets the brook name.
        /// </summary>
        public static string BrookName => "TEST.APP.BROOK";
    }

    /// <summary>
    ///     Constructor should throw when grain factory is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenGrainFactoryIsNull()
    {
        Mock<ILogger<AggregateGrainFactory>> loggerMock = new();
        Assert.Throws<ArgumentNullException>(() => new AggregateGrainFactory(null!, loggerMock.Object));
    }

    /// <summary>
    ///     Constructor should throw when logger is null.
    /// </summary>
    [Fact]
    public void ConstructorThrowsWhenLoggerIsNull()
    {
        Mock<IGrainFactory> grainFactoryMock = new();
        Assert.Throws<ArgumentNullException>(() => new AggregateGrainFactory(grainFactoryMock.Object, null!));
    }

    /// <summary>
    ///     GetAggregate with brook key should resolve grain with the key.
    /// </summary>
    [Fact]
    public void GetAggregateWithBrookKeyResolvesGrain()
    {
        Mock<IGrainFactory> grainFactoryMock = new();
        Mock<ILogger<AggregateGrainFactory>> loggerMock = new();
        Mock<ITestAggregateGrain> grainMock = new();
        BrookKey expectedKey = BrookKey.For<TestBrook>("entity-456");
        string? capturedKey = null;
        grainFactoryMock.Setup(f => f.GetGrain<ITestAggregateGrain>(It.IsAny<string>(), It.IsAny<string?>()))
            .Callback<string, string?>((
                key,
                _
            ) => capturedKey = key)
            .Returns(grainMock.Object);
        AggregateGrainFactory factory = new(grainFactoryMock.Object, loggerMock.Object);
        ITestAggregateGrain result = factory.GetAggregate<ITestAggregateGrain>(expectedKey);
        Assert.Same(grainMock.Object, result);
        Assert.Equal((string)expectedKey, capturedKey);
    }

    /// <summary>
    ///     GetAggregate with brook type should resolve grain with correct key.
    /// </summary>
    [Fact]
    public void GetAggregateWithBrookTypeResolvesGrainWithCorrectKey()
    {
        Mock<IGrainFactory> grainFactoryMock = new();
        Mock<ILogger<AggregateGrainFactory>> loggerMock = new();
        Mock<ITestAggregateGrain> grainMock = new();
        string? capturedKey = null;
        grainFactoryMock.Setup(f => f.GetGrain<ITestAggregateGrain>(It.IsAny<string>(), It.IsAny<string?>()))
            .Callback<string, string?>((
                key,
                _
            ) => capturedKey = key)
            .Returns(grainMock.Object);
        AggregateGrainFactory factory = new(grainFactoryMock.Object, loggerMock.Object);
        ITestAggregateGrain result = factory.GetAggregate<ITestAggregateGrain, TestBrook>("entity-123");
        Assert.Same(grainMock.Object, result);
        Assert.NotNull(capturedKey);
        BrookKey expectedKey = BrookKey.For<TestBrook>("entity-123");
        Assert.Equal((string)expectedKey, capturedKey);
    }
}

/// <summary>
///     Test aggregate grain interface for testing.
/// </summary>
[Alias("Mississippi.EventSourcing.Aggregates.Tests.ITestAggregateGrain")]
internal interface ITestAggregateGrain : IAggregateGrain;