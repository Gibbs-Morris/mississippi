using System.Net;
using System.Reflection;

using Microsoft.Azure.Cosmos;

using Mississippi.EventSourcing.Cosmos.Retry;


namespace Mississippi.EventSourcing.Cosmos.Tests.Retry;

/// <summary>
///     Tests for <see cref="Mississippi.EventSourcing.Cosmos.Retry.CosmosRetryPolicy" />.
/// </summary>
public class CosmosRetryPolicyTests
{
    private static CosmosException CreateCosmosException(
        HttpStatusCode statusCode,
        TimeSpan? retryAfter = null
    )
    {
        Type type = typeof(CosmosException);

        // Prefer a ctor that accepts HttpStatusCode
        ConstructorInfo[] ctors =
            type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        ConstructorInfo? ctor =
            ctors.FirstOrDefault(c => c.GetParameters().Any(p => p.ParameterType == typeof(HttpStatusCode)));
        if (ctor is null)
        {
            throw new InvalidOperationException("No suitable CosmosException constructor found for tests.");
        }

        ParameterInfo[] parameters = ctor.GetParameters();
        object?[] args = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            ParameterInfo p = parameters[i];
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
            else if (p.ParameterType == typeof(TimeSpan))
            {
                args[i] = retryAfter ?? TimeSpan.Zero;
            }
            else if (p.ParameterType.IsValueType)
            {
                args[i] = Activator.CreateInstance(p.ParameterType);
            }
            else
            {
                args[i] = null;
            }
        }

        CosmosException? instance = (CosmosException?)ctor.Invoke(args);
        if (instance is null)
        {
            throw new InvalidOperationException("Failed to construct CosmosException");
        }

        return instance;
    }

    /// <summary>
    ///     Verifies that TooManyRequests (429) is retried until the operation succeeds.
    /// </summary>
    /// <returns>A task representing the test execution.</returns>
    [Fact]
    public async Task ExecuteAsyncTooManyRequestsRetriesUntilSuccessAsync()
    {
        // Arrange
        int calls = 0;
        CosmosRetryPolicy policy = new();
        Func<Task<int>> operationAsync = () =>
        {
            calls++;
            if (calls < 3)
            {
                throw CreateCosmosException(HttpStatusCode.TooManyRequests, TimeSpan.FromMilliseconds(1));
            }

            return Task.FromResult(123);
        };

        // Act
        int result = await policy.ExecuteAsync(operationAsync);

        // Assert
        Assert.Equal(123, result);
        Assert.Equal(3, calls);
    }

    /// <summary>
    ///     Verifies that RequestEntityTooLarge results in an InvalidOperationException being thrown.
    /// </summary>
    /// <returns>A task representing the test execution.</returns>
    [Fact]
    public async Task ExecuteAsyncRequestEntityTooLargeThrowsInvalidOperationExceptionAsync()
    {
        // Arrange
        CosmosRetryPolicy policy = new(1);
        Func<Task<int>> operationAsync = () => throw CreateCosmosException(HttpStatusCode.RequestEntityTooLarge);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => policy.ExecuteAsync(operationAsync));
    }

    /// <summary>
    ///     Verifies that NotFound (404) passes through as a CosmosException.
    /// </summary>
    /// <returns>A task representing the test execution.</returns>
    [Fact]
    public async Task ExecuteAsyncNotFoundPassesThroughAsync()
    {
        // Arrange
        CosmosRetryPolicy policy = new(1);
        Func<Task<int>> operationAsyncNotFound = () => throw CreateCosmosException(HttpStatusCode.NotFound);

        // Act & Assert
        await Assert.ThrowsAsync<CosmosException>(() => policy.ExecuteAsync(operationAsyncNotFound));
    }

    /// <summary>
    ///     Verifies that TaskCanceledException is translated to OperationCanceledException without retries.
    /// </summary>
    /// <returns>A task representing the test execution.</returns>
    [Fact]
    public async Task ExecuteAsyncTaskCanceledEventuallyThrowsOperationCanceledExceptionAsync()
    {
        // Arrange
        int calls = 0;
        CosmosRetryPolicy policy = new(1);
        Func<Task<int>> operationAsyncCanceled = () =>
        {
            calls++;
            return Task.FromException<int>(new TaskCanceledException());
        };

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => policy.ExecuteAsync(operationAsyncCanceled));
        Assert.Equal(1, calls);
    }

    /// <summary>
    ///     Verifies that cancellation tokens are honored without additional retries.
    /// </summary>
    /// <returns>A task representing the test execution.</returns>
    [Fact]
    public async Task ExecuteAsyncHonorsCancellationTokenAsync()
    {
        // Arrange
        CosmosRetryPolicy policy = new(3);
        int calls = 0;
        using CancellationTokenSource cts = new();
        Func<Task<int>> operationAsync = () =>
        {
            calls++;
            cts.CancelAfter(TimeSpan.Zero);
            System.Threading.SpinWait.SpinUntil(() => cts.IsCancellationRequested);
            throw new TaskCanceledException();
        };

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => policy.ExecuteAsync(operationAsync, cts.Token));
        Assert.Equal(1, calls);
    }
}