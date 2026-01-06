using System;
using System.Diagnostics.CodeAnalysis;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.Logging.Abstractions;

using Mississippi.Ripples.Abstractions;


namespace Mississippi.Ripples.InProcess.L0Tests;

/// <summary>
///     Tests for <see cref="InProcessProjectionUpdateNotifier" />.
/// </summary>
[AllureParentSuite("Mississippi.Ripples.InProcess")]
[AllureSuite("Notifier")]
[AllureSubSuite("InProcessProjectionUpdateNotifier")]
public sealed class InProcessProjectionUpdateNotifierTests
{
    /// <summary>
    ///     Disposing a subscription stops callback invocation.
    /// </summary>
    [Fact]
    [AllureFeature("Disposal")]
    [SuppressMessage(
        "IDisposableAnalyzers.Correctness",
        "IDISP017:Prefer using",
        Justification = "Testing disposal behavior requires explicit Dispose call to verify callback stops")]
    public void DisposeStopsCallbackInvocation()
    {
        // Arrange
        InProcessProjectionUpdateNotifier sut = new(NullLogger<InProcessProjectionUpdateNotifier>.Instance);
        int invokeCount = 0;
        IDisposable subscription = sut.Subscribe("TestProjection", "entity-1", _ => invokeCount++);

        // Act
        subscription.Dispose();
        sut.NotifyProjectionChanged("TestProjection", "entity-1", 42);

        // Assert
        Assert.Equal(0, invokeCount);
    }

    /// <summary>
    ///     Disposing a subscription multiple times does not throw.
    /// </summary>
    [Fact]
    [AllureFeature("Disposal")]
    public void DisposeWhenCalledMultipleTimesDoesNotThrow()
    {
        // Arrange
        InProcessProjectionUpdateNotifier sut = new(NullLogger<InProcessProjectionUpdateNotifier>.Instance);
        IDisposable subscription = sut.Subscribe("TestProjection", "entity-1", _ => { });

        // Act & Assert (no exception)
        Exception? ex = Record.Exception(() =>
        {
            subscription.Dispose();
            subscription.Dispose();
        });
        Assert.Null(ex);
    }

    /// <summary>
    ///     NotifyProjectionChanged does not invoke callback when entity ID differs.
    /// </summary>
    [Fact]
    [AllureFeature("Notification")]
    public void NotifyProjectionChangedDoesNotInvokeCallbackForDifferentEntityId()
    {
        // Arrange
        InProcessProjectionUpdateNotifier sut = new(NullLogger<InProcessProjectionUpdateNotifier>.Instance);
        bool invoked = false;
        using IDisposable subscription = sut.Subscribe("TestProjection", "entity-1", _ => invoked = true);

        // Act
        sut.NotifyProjectionChanged("TestProjection", "entity-2", 42);

        // Assert
        Assert.False(invoked);
    }

    /// <summary>
    ///     NotifyProjectionChanged does not invoke callback when projection type differs.
    /// </summary>
    [Fact]
    [AllureFeature("Notification")]
    public void NotifyProjectionChangedDoesNotInvokeCallbackForDifferentProjectionType()
    {
        // Arrange
        InProcessProjectionUpdateNotifier sut = new(NullLogger<InProcessProjectionUpdateNotifier>.Instance);
        bool invoked = false;
        using IDisposable subscription = sut.Subscribe("TestProjection", "entity-1", _ => invoked = true);

        // Act
        sut.NotifyProjectionChanged("OtherProjection", "entity-1", 42);

        // Assert
        Assert.False(invoked);
    }

    /// <summary>
    ///     NotifyProjectionChanged invokes all callbacks when multiple subscriptions exist.
    /// </summary>
    [Fact]
    [AllureFeature("Notification")]
    public void NotifyProjectionChangedInvokesMultipleCallbacksForSameSubscription()
    {
        // Arrange
        InProcessProjectionUpdateNotifier sut = new(NullLogger<InProcessProjectionUpdateNotifier>.Instance);
        int invokeCount1 = 0;
        int invokeCount2 = 0;
        using IDisposable subscription1 = sut.Subscribe("TestProjection", "entity-1", _ => invokeCount1++);
        using IDisposable subscription2 = sut.Subscribe("TestProjection", "entity-1", _ => invokeCount2++);

        // Act
        sut.NotifyProjectionChanged("TestProjection", "entity-1", 42);

        // Assert
        Assert.Equal(1, invokeCount1);
        Assert.Equal(1, invokeCount2);
    }

    /// <summary>
    ///     NotifyProjectionChanged invokes the subscribed callback with correct event data.
    /// </summary>
    [Fact]
    [AllureFeature("Notification")]
    public void NotifyProjectionChangedInvokesSubscribedCallback()
    {
        // Arrange
        InProcessProjectionUpdateNotifier sut = new(NullLogger<InProcessProjectionUpdateNotifier>.Instance);
        ProjectionUpdatedEvent? received = null;
        using IDisposable subscription = sut.Subscribe("TestProjection", "entity-1", args => received = args);

        // Act
        sut.NotifyProjectionChanged("TestProjection", "entity-1", 42);

        // Assert
        Assert.NotNull(received);
        Assert.Equal("TestProjection", received.ProjectionType);
        Assert.Equal("entity-1", received.EntityId);
        Assert.Equal(42, received.NewVersion);
    }

    /// <summary>
    ///     NotifyProjectionChanged continues to other callbacks when one throws.
    /// </summary>
    [Fact]
    [AllureFeature("Notification")]
    public void NotifyProjectionChangedWhenCallbackThrowsContinuesToOtherCallbacks()
    {
        // Arrange
        InProcessProjectionUpdateNotifier sut = new(NullLogger<InProcessProjectionUpdateNotifier>.Instance);
        int invokeCount = 0;
        using IDisposable subscription1 = sut.Subscribe(
            "TestProjection",
            "entity-1",
            _ => throw new InvalidOperationException("Test exception"));
        using IDisposable subscription2 = sut.Subscribe("TestProjection", "entity-1", _ => invokeCount++);

        // Act
        sut.NotifyProjectionChanged("TestProjection", "entity-1", 42);

        // Assert
        Assert.Equal(1, invokeCount);
    }

    /// <summary>
    ///     NotifyProjectionChanged throws ArgumentNullException when entityId is null.
    /// </summary>
    [Fact]
    [AllureFeature("Notification")]
    public void NotifyProjectionChangedWithNullEntityIdThrowsArgumentNullException()
    {
        // Arrange
        InProcessProjectionUpdateNotifier sut = new(NullLogger<InProcessProjectionUpdateNotifier>.Instance);

        // Act & Assert
        ArgumentNullException ex =
            Assert.Throws<ArgumentNullException>(() => sut.NotifyProjectionChanged("TestProjection", null!, 42));
        Assert.Equal("entityId", ex.ParamName);
    }

    /// <summary>
    ///     NotifyProjectionChanged throws ArgumentNullException when projectionType is null.
    /// </summary>
    [Fact]
    [AllureFeature("Notification")]
    public void NotifyProjectionChangedWithNullProjectionTypeThrowsArgumentNullException()
    {
        // Arrange
        InProcessProjectionUpdateNotifier sut = new(NullLogger<InProcessProjectionUpdateNotifier>.Instance);

        // Act & Assert
        ArgumentNullException ex =
            Assert.Throws<ArgumentNullException>(() => sut.NotifyProjectionChanged(null!, "entity-1", 42));
        Assert.Equal("projectionType", ex.ParamName);
    }

    /// <summary>
    ///     Subscribe throws ArgumentException when projectionType is empty.
    /// </summary>
    [Fact]
    [AllureFeature("Subscription")]
    public void SubscribeWithEmptyProjectionTypeThrowsArgumentException()
    {
        // Arrange
        InProcessProjectionUpdateNotifier sut = new(NullLogger<InProcessProjectionUpdateNotifier>.Instance);

        // Act & Assert
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
        {
            using IDisposable subscription = sut.Subscribe(string.Empty, "entity-1", _ => { });
        });
        Assert.Equal("projectionType", ex.ParamName);
    }

    /// <summary>
    ///     Subscribe throws ArgumentNullException when callback is null.
    /// </summary>
    [Fact]
    [AllureFeature("Subscription")]
    public void SubscribeWithNullCallbackThrowsArgumentNullException()
    {
        // Arrange
        InProcessProjectionUpdateNotifier sut = new(NullLogger<InProcessProjectionUpdateNotifier>.Instance);

        // Act & Assert
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
        {
            using IDisposable subscription = sut.Subscribe("TestProjection", "entity-1", null!);
        });
        Assert.Equal("callback", ex.ParamName);
    }

    /// <summary>
    ///     Subscribe throws ArgumentNullException when entityId is null.
    /// </summary>
    [Fact]
    [AllureFeature("Subscription")]
    public void SubscribeWithNullEntityIdThrowsArgumentNullException()
    {
        // Arrange
        InProcessProjectionUpdateNotifier sut = new(NullLogger<InProcessProjectionUpdateNotifier>.Instance);

        // Act & Assert
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
        {
            using IDisposable subscription = sut.Subscribe("TestProjection", null!, _ => { });
        });
        Assert.Equal("entityId", ex.ParamName);
    }

    /// <summary>
    ///     Subscribe throws ArgumentNullException when projectionType is null.
    /// </summary>
    [Fact]
    [AllureFeature("Subscription")]
    public void SubscribeWithNullProjectionTypeThrowsArgumentNullException()
    {
        // Arrange
        InProcessProjectionUpdateNotifier sut = new(NullLogger<InProcessProjectionUpdateNotifier>.Instance);

        // Act & Assert
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
        {
            using IDisposable subscription = sut.Subscribe(null!, "entity-1", _ => { });
        });
        Assert.Equal("projectionType", ex.ParamName);
    }

    /// <summary>
    ///     Subscribe returns a non-null IDisposable when called with valid parameters.
    /// </summary>
    [Fact]
    [AllureFeature("Subscription")]
    public void SubscribeWithValidParametersReturnsDisposable()
    {
        // Arrange
        InProcessProjectionUpdateNotifier sut = new(NullLogger<InProcessProjectionUpdateNotifier>.Instance);

        // Act
        using IDisposable result = sut.Subscribe("TestProjection", "entity-1", _ => { });

        // Assert
        Assert.NotNull(result);
    }
}