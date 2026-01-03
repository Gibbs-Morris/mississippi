using System;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions;

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
        BrookKey expectedKey = new("TEST.APP.BROOK", "entity-456");
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
}

/// <summary>
///     Test aggregate grain interface for testing.
/// </summary>
[Alias("Mississippi.EventSourcing.Aggregates.Tests.ITestAggregateGrain")]
internal interface ITestAggregateGrain : IAggregateGrain;