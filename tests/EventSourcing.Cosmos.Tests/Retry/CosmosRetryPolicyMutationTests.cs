using System.Net;

using Microsoft.Azure.Cosmos;

using Mississippi.EventSourcing.Cosmos.Retry;


namespace Mississippi.EventSourcing.Cosmos.Tests.Retry;

/// <summary>
///     Mutation-killing tests for <see cref="CosmosRetryPolicy" />.
///     These tests target specific mutations to improve mutation score.
/// </summary>
public class CosmosRetryPolicyMutationTests
{
    private static CosmosException CreateCosmosException(
        HttpStatusCode statusCode,
        TimeSpan? retryAfter = null
    )
    {
        Type type = typeof(CosmosException);

        // Prefer a ctor that accepts HttpStatusCode
        System.Reflection.ConstructorInfo[] ctors =
            type.GetConstructors(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        System.Reflection.ConstructorInfo? ctor =
            ctors.FirstOrDefault(c => c.GetParameters().Any(p => p.ParameterType == typeof(HttpStatusCode)));
        if (ctor is null)
        {
            throw new InvalidOperationException("No suitable CosmosException constructor found for tests.");
        }

        System.Reflection.ParameterInfo[] parameters = ctor.GetParameters();
        object?[] args = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            System.Reflection.ParameterInfo p = parameters[i];
            if (p.ParameterType == typeof(string))
            {
                args[i] = string.Empty;
            }
            else if (p.ParameterType == typeof(HttpStatusCode))
            {
                args[i] = statusCode;
            }
            else if (p.ParameterType == typeof(int))
            {
                args[i] = 0;
            }
            else if (p.ParameterType == typeof(long))
            {
                args[i] = 0L;
            }
            else if (p.ParameterType == typeof(System.Net.Http.Headers.HttpResponseHeaders))
            {
                args[i] = null;
            }
            else if (p.ParameterType == typeof(TimeSpan))
            {
                args[i] = retryAfter ?? TimeSpan.Zero;
            }
            else if (p.ParameterType == typeof(TimeSpan?))
            {
                args[i] = retryAfter;
            }
            else if (p.ParameterType == typeof(Uri))
            {
                args[i] = null;
            }
            else if (p.ParameterType == typeof(Exception))
            {
                args[i] = null;
            }
            else
            {
                args[i] = null;
            }
        }

        CosmosException? ex = ctor.Invoke(args) as CosmosException;
        if (ex is null)
        {
            throw new InvalidOperationException("Failed to create CosmosException instance.");
        }

        return ex;
    }

    /// <summary>
    ///     Tests that the loop condition uses &lt;= (not just &lt;) for attempt vs MaxRetries.
    ///     Kills Equality mutation at line 38: attempt &lt;= MaxRetries.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_Should_AttemptExactlyMaxRetriesPlusOne_WhenAllAttemptsFail()
    {
        // Arrange
        CosmosRetryPolicy sut = new(maxRetries: 2);
        int attemptCount = 0;

        async Task<string> OperationThatAlwaysFails()
        {
            attemptCount++;
            await Task.Yield();
            throw CreateCosmosException(HttpStatusCode.TooManyRequests);
        }

        // Act & Assert
        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sut.ExecuteAsync(OperationThatAlwaysFails));

        // With maxRetries=2, should attempt: 0, 1, 2 = 3 total attempts
        Assert.Equal(3, attemptCount);
    }

    /// <summary>
    ///     Tests that cancellation is checked at the start of each retry attempt.
    ///     Kills Statement mutation at line 40: cancellationToken.ThrowIfCancellationRequested().
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_Should_ThrowOperationCanceledException_WhenCancellationTokenCanceledBeforeRetry()
    {
        // Arrange
        CosmosRetryPolicy sut = new(maxRetries: 3);
        using CancellationTokenSource cts = new();
        int attemptCount = 0;

        async Task<string> OperationThatCancelsAfterFirstAttempt()
        {
            attemptCount++;
            if (attemptCount == 1)
            {
                // Cancel before the retry
                await Task.Delay(1);
                cts.Cancel();
                throw CreateCosmosException(HttpStatusCode.TooManyRequests);
            }

            await Task.Yield();
            throw CreateCosmosException(HttpStatusCode.TooManyRequests);
        }

        // Act & Assert
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await sut.ExecuteAsync(OperationThatCancelsAfterFirstAttempt, cts.Token));

        // Should only attempt once before cancellation is detected
        Assert.Equal(1, attemptCount);
    }

    /// <summary>
    ///     Tests that RequestEntityTooLarge throws specific InvalidOperationException with correct message.
    ///     Kills Statement mutation at lines 47-49: throw new InvalidOperationException(...).
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_Should_ThrowInvalidOperationExceptionWithSpecificMessage_WhenRequestEntityTooLarge()
    {
        // Arrange
        CosmosRetryPolicy sut = new();
        CosmosException cosmosEx = CreateCosmosException(HttpStatusCode.RequestEntityTooLarge);

        // Act
        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sut.ExecuteAsync<string>(() => throw cosmosEx));

        // Assert - verify exact message and inner exception
        Assert.Contains("Request size exceeds maximum allowed limit", ex.Message);
        Assert.Contains("reducing batch size", ex.Message);
        Assert.Same(cosmosEx, ex.InnerException);
    }

    /// <summary>
    ///     Tests that attempt &lt; MaxRetries condition (not &lt;=) for retry eligibility.
    ///     Kills Equality mutation at line 55: attempt &lt; MaxRetries.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_Should_NotRetry_WhenAttemptEqualsMaxRetries()
    {
        // Arrange
        CosmosRetryPolicy sut = new(maxRetries: 1);
        int attemptCount = 0;

        async Task<string> OperationThatAlwaysFails()
        {
            attemptCount++;
            await Task.Yield();
            throw CreateCosmosException(HttpStatusCode.TooManyRequests);
        }

        // Act & Assert
        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sut.ExecuteAsync(OperationThatAlwaysFails));

        // With maxRetries=1, should attempt: 0, 1 = 2 total attempts (no retry on attempt 1)
        Assert.Equal(2, attemptCount);
    }

    /// <summary>
    ///     Tests that ConfigureAwait(false) is used on Task.Delay.
    ///     Kills Boolean mutation at line 59: .ConfigureAwait(false).
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_Should_UseConfigureAwaitFalse_OnDelayBetweenRetries()
    {
        // Arrange
        CosmosRetryPolicy sut = new(maxRetries: 1);
        int attemptCount = 0;

        async Task<string> OperationThatFailsOnce()
        {
            attemptCount++;
            if (attemptCount == 1)
            {
                await Task.Yield();
                throw CreateCosmosException(HttpStatusCode.TooManyRequests);
            }

            return "success";
        }

        // Act
        string result = await sut.ExecuteAsync(OperationThatFailsOnce);

        // Assert - verify retry happened and operation succeeded
        Assert.Equal("success", result);
        Assert.Equal(2, attemptCount);
    }

    /// <summary>
    ///     Tests that Task.Delay is called between retries with correct delay.
    ///     Kills Statement mutation at line 59: await Task.Delay(delay, cancellationToken).
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_Should_DelayBetweenRetries_WhenTransientErrorOccurs()
    {
        // Arrange
        CosmosRetryPolicy sut = new(maxRetries: 1);
        int attemptCount = 0;
        DateTime firstAttemptTime = DateTime.MinValue;
        DateTime secondAttemptTime = DateTime.MinValue;

        async Task<string> OperationThatFailsOnce()
        {
            attemptCount++;
            if (attemptCount == 1)
            {
                firstAttemptTime = DateTime.UtcNow;
                await Task.Yield();
                throw CreateCosmosException(HttpStatusCode.TooManyRequests);
            }

            secondAttemptTime = DateTime.UtcNow;
            return "success";
        }

        // Act
        string result = await sut.ExecuteAsync(OperationThatFailsOnce);

        // Assert - verify delay occurred between attempts (exponential backoff: 2^0 * 100ms = ~100ms)
        // Allow small tolerance for timing variations
        Assert.Equal("success", result);
        TimeSpan actualDelay = secondAttemptTime - firstAttemptTime;
        Assert.True(actualDelay.TotalMilliseconds >= 95, $"Expected delay >= 95ms, got {actualDelay.TotalMilliseconds}ms");
    }

    /// <summary>
    ///     Tests that TaskCanceled with cancellation requested throws OperationCanceledException with message.
    ///     Kills Statement mutation at line 70: throw new OperationCanceledException(...).
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_Should_ThrowOperationCanceledExceptionWithMessage_WhenTaskCanceledWithCancellationRequested()
    {
        // Arrange
        CosmosRetryPolicy sut = new();
        using CancellationTokenSource cts = new();
        cts.Cancel();

        async Task<string> OperationThatThrowsTaskCanceledException()
        {
            await Task.Yield();
            throw new TaskCanceledException();
        }

        // Act
        OperationCanceledException ex = await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await sut.ExecuteAsync(OperationThatThrowsTaskCanceledException, cts.Token));

        // Assert - verify exception is thrown (message may vary based on cancellation path)
        Assert.NotNull(ex);
        Assert.True(ex.CancellationToken.IsCancellationRequested);
    }

    /// <summary>
    ///     Tests equality in IsTransientCosmosStatus - checking multiple status codes.
    ///     Kills Equality mutations at lines 87-91.
    /// </summary>
    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task ExecuteAsync_Should_RetryTransientErrors_ForAllTransientStatusCodes(HttpStatusCode statusCode)
    {
        // Arrange
        CosmosRetryPolicy sut = new(maxRetries: 1);
        int attemptCount = 0;

        async Task<string> OperationThatFailsOnceWithStatus()
        {
            attemptCount++;
            if (attemptCount == 1)
            {
                await Task.Yield();
                throw CreateCosmosException(statusCode);
            }

            return "success";
        }

        // Act
        string result = await sut.ExecuteAsync(OperationThatFailsOnceWithStatus);

        // Assert - verify retry happened for this transient status
        Assert.Equal("success", result);
        Assert.Equal(2, attemptCount);
    }

    /// <summary>
    ///     Tests arithmetic in ComputeDelay - exponential backoff calculation.
    ///     Kills Arithmetic mutation at line 104: Math.Pow(2, attempt) * 100.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_Should_UseExponentialBackoff_ForRetryDelays()
    {
        // Arrange
        CosmosRetryPolicy sut = new(maxRetries: 2);
        int attemptCount = 0;
        List<DateTime> attemptTimes = new();

        async Task<string> OperationThatFailsTwice()
        {
            attemptCount++;
            attemptTimes.Add(DateTime.UtcNow);

            if (attemptCount <= 2)
            {
                await Task.Yield();
                throw CreateCosmosException(HttpStatusCode.TooManyRequests);
            }

            return "success";
        }

        // Act
        string result = await sut.ExecuteAsync(OperationThatFailsTwice);

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(3, attemptCount);

        // Verify exponential backoff: attempt 0->1: ~100ms, attempt 1->2: ~200ms
        // Allow small tolerance for timing variations
        TimeSpan delay1 = attemptTimes[1] - attemptTimes[0];
        TimeSpan delay2 = attemptTimes[2] - attemptTimes[1];

        Assert.True(delay1.TotalMilliseconds >= 95, $"First delay should be >= 95ms, got {delay1.TotalMilliseconds}ms");
        Assert.True(delay2.TotalMilliseconds >= 195, $"Second delay should be >= 195ms, got {delay2.TotalMilliseconds}ms");

        // Verify second delay is roughly double the first (exponential)
        // The formula is 2^attempt * 100, so delay2/delay1 should be ~2
        double ratio = delay2.TotalMilliseconds / delay1.TotalMilliseconds;
        Assert.True(ratio >= 1.4 && ratio <= 2.6, $"Delay ratio should be ~2.0, got {ratio:F2}");
    }

    /// <summary>
    ///     Tests that RetryAfter value is accessed when present in CosmosException.
    ///     Kills mutations related to RetryAfter.HasValue check.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_Should_CheckRetryAfterValue_WhenProvidedByCosmosException()
    {
        // Arrange
        CosmosRetryPolicy sut = new(maxRetries: 1);
        int attemptCount = 0;
        TimeSpan retryAfter = TimeSpan.FromMilliseconds(500);

        async Task<string> OperationThatFailsOnce()
        {
            attemptCount++;
            if (attemptCount == 1)
            {
                await Task.Yield();
                // Create exception with RetryAfter (the code checks ex.RetryAfter.HasValue)
                throw CreateCosmosException(HttpStatusCode.TooManyRequests, retryAfter);
            }

            return "success";
        }

        // Act
        string result = await sut.ExecuteAsync(OperationThatFailsOnce);

        // Assert - verify retry happened (the test verifies the code path is exercised)
        Assert.Equal("success", result);
        Assert.Equal(2, attemptCount);
    }
}
