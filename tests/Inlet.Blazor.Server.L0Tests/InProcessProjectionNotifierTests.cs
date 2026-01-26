using System;
using System.Diagnostics.CodeAnalysis;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.Inlet.Client.Abstractions;


namespace Mississippi.Inlet.Blazor.Server.L0Tests;

/// <summary>
///     Tests for <see cref="InProcessProjectionNotifier" />.
/// </summary>
[AllureParentSuite("Mississippi.Inlet.Blazor.Server")]
[AllureSuite("Notifiers")]
[AllureSubSuite("InProcessProjectionNotifier")]
public sealed class InProcessProjectionNotifierTests : IDisposable
{
    private readonly IServerProjectionNotifier notifier;

    private readonly ServiceProvider serviceProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InProcessProjectionNotifierTests" /> class.
    /// </summary>
    public InProcessProjectionNotifierTests()
    {
        ServiceCollection services = [];
        services.AddLogging();
        services.AddInletInProcess();
        serviceProvider = services.BuildServiceProvider();
        notifier = serviceProvider.GetRequiredService<IServerProjectionNotifier>();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        serviceProvider.Dispose();
    }

    /// <summary>
    ///     NotifyProjectionChanged should continue invoking other callbacks when one throws.
    /// </summary>
    [Fact]
    [AllureFeature("Notification")]
    public void NotifyProjectionChangedContinuesWhenCallbackThrows()
    {
        // Arrange
        bool secondCallbackInvoked = false;
        using IDisposable subscription1 = notifier.Subscribe(
            "TestProjection",
            "entity-1",
            _ => throw new InvalidOperationException("Test exception"));
        using IDisposable subscription2 = notifier.Subscribe(
            "TestProjection",
            "entity-1",
            _ => secondCallbackInvoked = true);

        // Act
        notifier.NotifyProjectionChanged("TestProjection", "entity-1", 1);

        // Assert
        Assert.True(secondCallbackInvoked);
    }

    /// <summary>
    ///     NotifyProjectionChanged should not invoke disposed subscription callback.
    /// </summary>
    [Fact]
    [AllureFeature("Notification")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP017:Prefer using",
        Justification = "Testing explicit disposal behavior")]
    public void NotifyProjectionChangedDoesNotInvokeDisposedSubscription()
    {
        // Arrange
        bool callbackInvoked = false;
        IDisposable subscription = notifier.Subscribe("TestProjection", "entity-1", _ => callbackInvoked = true);
        subscription.Dispose();

        // Act
        notifier.NotifyProjectionChanged("TestProjection", "entity-1", 1);

        // Assert
        Assert.False(callbackInvoked);
    }

    /// <summary>
    ///     NotifyProjectionChanged should not invoke subscriber for different entity.
    /// </summary>
    [Fact]
    [AllureFeature("Notification")]
    public void NotifyProjectionChangedDoesNotInvokeForDifferentEntity()
    {
        // Arrange
        bool callbackInvoked = false;
        using IDisposable subscription = notifier.Subscribe("TestProjection", "entity-1", _ => callbackInvoked = true);

        // Act
        notifier.NotifyProjectionChanged("TestProjection", "entity-2", 1);

        // Assert
        Assert.False(callbackInvoked);
    }

    /// <summary>
    ///     NotifyProjectionChanged should not invoke subscriber for different projection type.
    /// </summary>
    [Fact]
    [AllureFeature("Notification")]
    public void NotifyProjectionChangedDoesNotInvokeForDifferentProjectionType()
    {
        // Arrange
        bool callbackInvoked = false;
        using IDisposable subscription = notifier.Subscribe("TestProjection", "entity-1", _ => callbackInvoked = true);

        // Act
        notifier.NotifyProjectionChanged("OtherProjection", "entity-1", 1);

        // Assert
        Assert.False(callbackInvoked);
    }

    /// <summary>
    ///     NotifyProjectionChanged should not invoke callback when no subscriptions.
    /// </summary>
    [Fact]
    [AllureFeature("Notification")]
    [SuppressMessage(
        "SonarQube",
        "S2699:Tests should include assertions",
        Justification = "This test verifies no exception is thrown when notifying without subscribers")]
    public void NotifyProjectionChangedDoesNothingWithNoSubscribers()
    {
        // Act - should not throw
        notifier.NotifyProjectionChanged("TestProjection", "entity-1", 1);

        // Assert - no exception means success
        Assert.True(true);
    }

    /// <summary>
    ///     NotifyProjectionChanged should invoke subscriber callback.
    /// </summary>
    [Fact]
    [AllureFeature("Notification")]
    public void NotifyProjectionChangedInvokesCallback()
    {
        // Arrange
        ProjectionUpdatedEvent? receivedEvent = null;
        using IDisposable subscription = notifier.Subscribe("TestProjection", "entity-1", e => receivedEvent = e);

        // Act
        notifier.NotifyProjectionChanged("TestProjection", "entity-1", 42);

        // Assert
        Assert.NotNull(receivedEvent);
        Assert.Equal("TestProjection", receivedEvent.ProjectionType);
        Assert.Equal("entity-1", receivedEvent.EntityId);
        Assert.Equal(42, receivedEvent.NewVersion);
    }

    /// <summary>
    ///     NotifyProjectionChanged should invoke multiple subscribers.
    /// </summary>
    [Fact]
    [AllureFeature("Notification")]
    public void NotifyProjectionChangedInvokesMultipleSubscribers()
    {
        // Arrange
        int callCount = 0;
        using IDisposable subscription1 = notifier.Subscribe("TestProjection", "entity-1", _ => callCount++);
        using IDisposable subscription2 = notifier.Subscribe("TestProjection", "entity-1", _ => callCount++);

        // Act
        notifier.NotifyProjectionChanged("TestProjection", "entity-1", 1);

        // Assert
        Assert.Equal(2, callCount);
    }

    /// <summary>
    ///     NotifyProjectionChanged should throw when entityId is empty.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void NotifyProjectionChangedThrowsWhenEntityIdEmpty()
    {
        // Act & Assert
        ArgumentException exception =
            Assert.Throws<ArgumentException>(() => notifier.NotifyProjectionChanged("TestProjection", string.Empty, 1));
        Assert.Equal("entityId", exception.ParamName);
    }

    /// <summary>
    ///     NotifyProjectionChanged should throw when entityId is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void NotifyProjectionChangedThrowsWhenEntityIdNull()
    {
        // Act & Assert
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => notifier.NotifyProjectionChanged("TestProjection", null!, 1));
        Assert.Equal("entityId", exception.ParamName);
    }

    /// <summary>
    ///     NotifyProjectionChanged should throw when projectionType is empty.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void NotifyProjectionChangedThrowsWhenProjectionTypeEmpty()
    {
        // Act & Assert
        ArgumentException exception =
            Assert.Throws<ArgumentException>(() => notifier.NotifyProjectionChanged(string.Empty, "entity-1", 1));
        Assert.Equal("projectionType", exception.ParamName);
    }

    /// <summary>
    ///     NotifyProjectionChanged should throw when projectionType is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    public void NotifyProjectionChangedThrowsWhenProjectionTypeNull()
    {
        // Act & Assert
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => notifier.NotifyProjectionChanged(null!, "entity-1", 1));
        Assert.Equal("projectionType", exception.ParamName);
    }

    /// <summary>
    ///     Subscribe should return a disposable subscription.
    /// </summary>
    [Fact]
    [AllureFeature("Subscription")]
    public void SubscribeReturnsDisposable()
    {
        // Act
        using IDisposable subscription = notifier.Subscribe("TestProjection", "entity-1", _ => { });

        // Assert
        Assert.NotNull(subscription);
    }

    /// <summary>
    ///     Subscribe should throw when callback is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Lambda expression throws before returning")]
    public void SubscribeThrowsWhenCallbackNull()
    {
        // Act & Assert
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => notifier.Subscribe("TestProjection", "entity-1", null!));
        Assert.Equal("callback", exception.ParamName);
    }

    /// <summary>
    ///     Subscribe should throw when entityId is empty.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Lambda expression throws before returning")]
    public void SubscribeThrowsWhenEntityIdEmpty()
    {
        // Act & Assert
        ArgumentException exception =
            Assert.Throws<ArgumentException>(() => notifier.Subscribe("TestProjection", string.Empty, _ => { }));
        Assert.Equal("entityId", exception.ParamName);
    }

    /// <summary>
    ///     Subscribe should throw when entityId is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Lambda expression throws before returning")]
    public void SubscribeThrowsWhenEntityIdNull()
    {
        // Act & Assert
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => notifier.Subscribe("TestProjection", null!, _ => { }));
        Assert.Equal("entityId", exception.ParamName);
    }

    /// <summary>
    ///     Subscribe should throw when projectionType is empty.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Lambda expression throws before returning")]
    public void SubscribeThrowsWhenProjectionTypeEmpty()
    {
        // Act & Assert
        ArgumentException exception =
            Assert.Throws<ArgumentException>(() => notifier.Subscribe(string.Empty, "entity-1", _ => { }));
        Assert.Equal("projectionType", exception.ParamName);
    }

    /// <summary>
    ///     Subscribe should throw when projectionType is null.
    /// </summary>
    [Fact]
    [AllureFeature("Argument Validation")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP005:Return type should indicate that the value should be disposed",
        Justification = "Lambda expression throws before returning")]
    public void SubscribeThrowsWhenProjectionTypeNull()
    {
        // Act & Assert
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() => notifier.Subscribe(null!, "entity-1", _ => { }));
        Assert.Equal("projectionType", exception.ParamName);
    }

    /// <summary>
    ///     Subscription collection should be cleaned up when all subscriptions are disposed.
    /// </summary>
    [Fact]
    [AllureFeature("Subscription")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP017:Prefer using",
        Justification = "Testing explicit disposal behavior")]
    public void SubscriptionCollectionCleanedUpWhenEmpty()
    {
        // Arrange
        bool callbackInvoked = false;
        IDisposable subscription = notifier.Subscribe("TestProjection", "entity-1", _ => callbackInvoked = true);

        // Act
        subscription.Dispose();

        // Re-subscribe and verify this is a fresh collection
        using IDisposable newSubscription = notifier.Subscribe(
            "TestProjection",
            "entity-1",
            _ => callbackInvoked = true);
        notifier.NotifyProjectionChanged("TestProjection", "entity-1", 1);

        // Assert
        Assert.True(callbackInvoked);
    }

    /// <summary>
    ///     Subscription dispose can be called multiple times.
    /// </summary>
    [Fact]
    [AllureFeature("Subscription")]
    [SuppressMessage(
        "SonarQube",
        "S2699:Tests should include assertions",
        Justification = "This test verifies no exception is thrown on multiple dispose calls")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP016:Don't use disposed instance",
        Justification = "Testing that multiple Dispose calls don't throw")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP017:Prefer using",
        Justification = "Testing explicit Dispose calls, not automatic disposal")]
    public void SubscriptionDisposeCanBeCalledMultipleTimes()
    {
        // Arrange
        IDisposable subscription = notifier.Subscribe("TestProjection", "entity-1", _ => { });

        // Act & Assert - should not throw
        subscription.Dispose();
        subscription.Dispose();
        Assert.True(true);
    }
}