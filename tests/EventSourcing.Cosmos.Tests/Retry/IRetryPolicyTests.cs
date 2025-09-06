using System.Net;
using System.Reflection;

using Microsoft.Azure.Cosmos;

using Mississippi.EventSourcing.Cosmos.Retry;


namespace Mississippi.EventSourcing.Cosmos.Tests.Retry;

/// <summary>
///     Contract-level tests for <see cref="IRetryPolicy" /> behavior using the Cosmos implementation.
/// </summary>
public sealed class IRetryPolicyTests
{
    /// <summary>
    ///     Verifies that transient Cosmos errors are retried and the successful result is returned.
    /// </summary>
    /// <returns>An asynchronous test operation.</returns>
    [Fact]
    public async Task ExecuteAsyncRetriesTransientAndReturnsResultAsync()
    {
        // Arrange
        int calls = 0;
        CosmosRetryPolicy policy = new();
        Func<Task<int>> operation = () =>
        {
            calls++;
            if (calls < 3)
            {
                throw CreateCosmosException(HttpStatusCode.TooManyRequests, TimeSpan.FromMilliseconds(1));
            }

            return Task.FromResult(42);
        };

        // Act
        int result = await policy.ExecuteAsync(operation);

        // Assert
        Assert.Equal(42, result);
        Assert.Equal(3, calls);
    }

    /// <summary>
    ///     Verifies that non-transient Cosmos errors are wrapped in InvalidOperationException.
    /// </summary>
    /// <returns>An asynchronous test operation.</returns>
    [Fact]
    public async Task ExecuteAsyncWrapsNonTransientCosmosErrorsAsync()
    {
        // Arrange
        CosmosRetryPolicy policy = new(1);
        Func<Task<int>> operation = () => Task.FromException<int>(CreateCosmosException(HttpStatusCode.BadRequest));

        // Act
        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => policy.ExecuteAsync(operation));

        // Assert
        Assert.IsType<CosmosException>(ex.InnerException);
        CosmosException inner = (CosmosException)ex.InnerException!;
        Assert.Equal(HttpStatusCode.BadRequest, inner.StatusCode);
    }

    private static CosmosException CreateCosmosException(
        HttpStatusCode statusCode,
        TimeSpan? retryAfter = null
    )
    {
        Type type = typeof(CosmosException);
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
}