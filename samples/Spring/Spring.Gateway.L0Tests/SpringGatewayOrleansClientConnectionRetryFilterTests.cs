using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Moq;

using Orleans.Runtime;
using Orleans.Runtime.Messaging;


namespace MississippiSamples.Spring.Gateway.L0Tests;

/// <summary>
///     Verifies bounded Spring gateway startup retries and shutdown cancellation.
/// </summary>
public sealed class SpringGatewayOrleansClientConnectionRetryFilterTests
{
    private static bool HasLogProperty(
        object state,
        string name,
        object expected
    ) =>
        state is IEnumerable<KeyValuePair<string, object?>> properties &&
        properties.Any(property => (property.Key == name) && Equals(property.Value, expected));

    /// <summary>
    ///     Verifies that a canceled call does not consume the startup retry budget.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CanceledCallShouldNotConsumeRetryBudgetAsync()
    {
        FakeTimeProvider timeProvider = new();
        SpringGatewayOrleansClientConnectionRetryFilter filter = new(
            timeProvider,
            NullLogger<SpringGatewayOrleansClientConnectionRetryFilter>.Instance);
        Exception failure = new ConnectionFailedException("No gateway is available.");
        Assert.False(await filter.ShouldRetryConnectionAttempt(failure, new(true)));
        for (int retryNumber = 0; retryNumber < 180; retryNumber++)
        {
            Task<bool> retry = filter.ShouldRetryConnectionAttempt(failure, CancellationToken.None);
            timeProvider.Advance(TimeSpan.FromSeconds(1));
            Assert.True(await retry);
        }

        Task<bool> exhausted = filter.ShouldRetryConnectionAttempt(failure, CancellationToken.None);
        Assert.True(exhausted.IsCompletedSuccessfully);
        Assert.False(await exhausted);
    }

    /// <summary>
    ///     Verifies that shutdown cancellation interrupts a pending delay without throwing.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task CancellationDuringDelayShouldStopRetryingAsync()
    {
        FakeTimeProvider timeProvider = new();
        using CancellationTokenSource cancellation = new();
        Mock<ILogger<SpringGatewayOrleansClientConnectionRetryFilter>> logger = new();
        logger.Setup(value => value.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        SpringGatewayOrleansClientConnectionRetryFilter filter = new(timeProvider, logger.Object);
        Task<bool> retry = filter.ShouldRetryConnectionAttempt(
            new ConnectionFailedException("No gateway is available."),
            cancellation.Token);
        Assert.False(retry.IsCompleted);
        await cancellation.CancelAsync();
        Assert.False(await retry);
        logger.Verify(
            value => value.Log(
                LogLevel.Debug,
                It.Is<EventId>(id => (id.Id == 4) && (id.Name == "ConnectionRetryCanceled")),
                It.Is<It.IsAnyType>((state, _) => HasLogProperty(
                    state,
                    "{OriginalFormat}",
                    "Spring gateway Orleans connection retry was canceled.")),
                It.IsAny<OperationCanceledException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    ///     Verifies that the injected dependencies are required.
    /// </summary>
    [Fact]
    public void ConstructorShouldRejectMissingDependencies()
    {
        Assert.Throws<ArgumentNullException>(() => new SpringGatewayOrleansClientConnectionRetryFilter(
            null!,
            NullLogger<SpringGatewayOrleansClientConnectionRetryFilter>.Instance));
        Assert.Throws<ArgumentNullException>(() =>
            new SpringGatewayOrleansClientConnectionRetryFilter(new FakeTimeProvider(), null!));
    }

    /// <summary>
    ///     Verifies that disabled diagnostics do not change the retry result.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task DisabledLoggingShouldPreserveRetryBehaviorAsync()
    {
        FakeTimeProvider timeProvider = new();
        Mock<ILogger<SpringGatewayOrleansClientConnectionRetryFilter>> logger = new();
        SpringGatewayOrleansClientConnectionRetryFilter filter = new(timeProvider, logger.Object);
        Task<bool> retry = filter.ShouldRetryConnectionAttempt(
            new ConnectionFailedException("No gateway is available."),
            CancellationToken.None);
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        Assert.True(await retry);
        logger.Verify(
            value => value.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    /// <summary>
    ///     Verifies that diagnostics preserve the startup failure and structured retry decision.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task EnabledLoggingShouldRecordFailureAndDecisionAsync()
    {
        FakeTimeProvider timeProvider = new();
        Mock<ILogger<SpringGatewayOrleansClientConnectionRetryFilter>> logger = new();
        logger.Setup(value => value.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        SpringGatewayOrleansClientConnectionRetryFilter filter = new(timeProvider, logger.Object);
        Exception failure = new ConnectionFailedException("No gateway is available.");
        Task<bool> retry = filter.ShouldRetryConnectionAttempt(failure, CancellationToken.None);
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        Assert.True(await retry);
        logger.Verify(
            value => value.Log(
                LogLevel.Debug,
                It.Is<EventId>(id => (id.Id == 1) && (id.Name == "ConnectionRetryStarted")),
                It.Is<It.IsAnyType>((state, _) => HasLogProperty(
                    state,
                    "{OriginalFormat}",
                    "Spring gateway is evaluating an Orleans startup connection failure.")),
                failure,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        logger.Verify(
            value => value.Log(
                LogLevel.Debug,
                It.Is<EventId>(id => (id.Id == 2) && (id.Name == "ConnectionRetryCompleted")),
                It.Is<It.IsAnyType>((state, _) => HasLogProperty(state, "ShouldRetry", true)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    /// <summary>
    ///     Verifies that the retry cap is permanent for the current client startup.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task ExhaustedBudgetShouldContinueRejectingRetriesAsync()
    {
        FakeTimeProvider timeProvider = new();
        Mock<ILogger<SpringGatewayOrleansClientConnectionRetryFilter>> logger = new();
        logger.Setup(value => value.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        SpringGatewayOrleansClientConnectionRetryFilter filter = new(timeProvider, logger.Object);
        Exception failure = new ConnectionFailedException("No gateway is available.");
        long startedTimestamp = timeProvider.GetTimestamp();
        for (int retryNumber = 0; retryNumber < 180; retryNumber++)
        {
            Task<bool> retry = filter.ShouldRetryConnectionAttempt(failure, CancellationToken.None);
            timeProvider.Advance(TimeSpan.FromSeconds(1));
            Assert.True(await retry);
        }

        Assert.Equal(TimeSpan.FromMinutes(3), timeProvider.GetElapsedTime(startedTimestamp));
        Task<bool> firstRejected = filter.ShouldRetryConnectionAttempt(failure, CancellationToken.None);
        Assert.True(firstRejected.IsCompletedSuccessfully);
        Assert.False(await firstRejected);
        Task<bool> secondRejected = filter.ShouldRetryConnectionAttempt(failure, CancellationToken.None);
        Assert.True(secondRejected.IsCompletedSuccessfully);
        Assert.False(await secondRejected);
        logger.Verify(
            value => value.Log(
                LogLevel.Warning,
                It.Is<EventId>(id => (id.Id == 3) && (id.Name == "ConnectionRetriesExhausted")),
                It.Is<It.IsAnyType>((state, _) =>
                    HasLogProperty(state, "MaxRetries", 180) &&
                    HasLogProperty(
                        state,
                        "{OriginalFormat}",
                        "Spring gateway exhausted its {MaxRetries} Orleans connection retries.")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    /// <summary>
    ///     Verifies that a missing failure does not schedule a retry delay.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task MissingFailureShouldStopWithoutDelayAsync()
    {
        SpringGatewayOrleansClientConnectionRetryFilter filter = new(
            new FakeTimeProvider(),
            NullLogger<SpringGatewayOrleansClientConnectionRetryFilter>.Instance);
        Task<bool> retry = filter.ShouldRetryConnectionAttempt(null!, CancellationToken.None);
        Assert.True(retry.IsCompletedSuccessfully);
        Assert.False(await retry);
    }

    /// <summary>
    ///     Verifies that permanent configuration and credential failures stop without a delay.
    /// </summary>
    /// <param name="authenticationFailure">Whether to use a credential failure.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PermanentFailureShouldStopWithoutDelayAsync(
        bool authenticationFailure
    )
    {
        SpringGatewayOrleansClientConnectionRetryFilter filter = new(
            new FakeTimeProvider(),
            NullLogger<SpringGatewayOrleansClientConnectionRetryFilter>.Instance);
        Exception failure = authenticationFailure
            ? new AuthenticationException("Clustering credentials are invalid.")
            : new InvalidOperationException("Clustering configuration is invalid.");
        Task<bool> retry = filter.ShouldRetryConnectionAttempt(failure, CancellationToken.None);
        Assert.True(retry.IsCompletedSuccessfully);
        Assert.False(await retry);
    }

    /// <summary>
    ///     Verifies the one-second delay before retrying any startup failure.
    /// </summary>
    /// <returns>A task representing the test.</returns>
    [Fact]
    public async Task StartupFailureShouldRetryOnlyAfterOneSecondAsync()
    {
        FakeTimeProvider timeProvider = new();
        SpringGatewayOrleansClientConnectionRetryFilter filter = new(
            timeProvider,
            NullLogger<SpringGatewayOrleansClientConnectionRetryFilter>.Instance);
        Task<bool> retry = filter.ShouldRetryConnectionAttempt(
            new ConnectionFailedException("No gateway is available."),
            CancellationToken.None);
        Assert.False(retry.IsCompleted);
        timeProvider.Advance(TimeSpan.FromMilliseconds(999));
        Assert.False(retry.IsCompleted);
        timeProvider.Advance(TimeSpan.FromMilliseconds(1));
        Assert.True(await retry);
    }

    /// <summary>
    ///     Verifies the retry delay for both eligible Orleans failure types.
    /// </summary>
    /// <param name="messageRejected">Whether to use a message rejection.</param>
    /// <returns>A task representing the test.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TransientFailureShouldRetryAfterDelayAsync(
        bool messageRejected
    )
    {
        FakeTimeProvider timeProvider = new();
        SpringGatewayOrleansClientConnectionRetryFilter filter = new(
            timeProvider,
            NullLogger<SpringGatewayOrleansClientConnectionRetryFilter>.Instance);
        Exception failure = messageRejected
            ? (OrleansMessageRejectionException)typeof(OrleansMessageRejectionException).GetConstructor(
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                [typeof(string)],
                null)!.Invoke(["The gateway is not ready."])
            : new ConnectionFailedException("No gateway is available.");
        Task<bool> retry = filter.ShouldRetryConnectionAttempt(failure, CancellationToken.None);
        Assert.False(retry.IsCompleted);
        timeProvider.Advance(TimeSpan.FromSeconds(1));
        Assert.True(await retry);
    }
}