using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;

using Mississippi.Core.Cosmos.Retry;


namespace Mississippi.Core.Cosmos.L0Tests;

/// <summary>
///     Tests for <see cref="CosmosRetryPolicy" />.
/// </summary>
public class CosmosRetryPolicyTests
{
    /// <summary>
    ///     Creates a <see cref="CosmosException" /> using the first available constructor that accepts
    ///     <see cref="HttpStatusCode" />.
    /// </summary>
    /// <returns>A constructed <see cref="CosmosException" /> for the specified status.</returns>
    private static CosmosException CreateCosmosException(
        HttpStatusCode statusCode
    )
    {
        ConstructorInfo[] constructors =
            typeof(CosmosException).GetConstructors(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        ConstructorInfo? target =
            constructors.FirstOrDefault(c => c.GetParameters().Any(p => p.ParameterType == typeof(HttpStatusCode)));
        if (target is null)
        {
            throw new InvalidOperationException("No CosmosException constructor accepting HttpStatusCode was found.");
        }

        object?[] parameters = target.GetParameters()
            .Select(parameter => parameter.ParameterType switch
            {
                Type p when p == typeof(string) => string.Empty,
                Type p when p == typeof(HttpStatusCode) => statusCode,
                Type p when p == typeof(int) => 0,
                Type p when p == typeof(long) => 0L,
                Type p when p == typeof(TimeSpan) => TimeSpan.Zero,
                { IsValueType: true } p => Activator.CreateInstance(p),
                var _ => null,
            })
            .ToArray();
        return (CosmosException?)target.Invoke(parameters) ??
               throw new InvalidOperationException("Failed to construct CosmosException.");
    }

    /// <summary>
    ///     Verifies not found errors pass through without retries.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test execution.</returns>
    [Fact]
    public async Task ExecuteAsyncPassesThroughNotFoundAsync()
    {
        CosmosRetryPolicy policy = new();
        await Assert.ThrowsAsync<CosmosException>(() =>
            policy.ExecuteAsync(() => Task.FromException<int>(CreateCosmosException(HttpStatusCode.NotFound))));
    }

    /// <summary>
    ///     Verifies transient failures are retried once before succeeding.
    /// </summary>
    /// <returns>A <see cref="Task" /> representing the asynchronous test execution.</returns>
    [Fact]
    public async Task ExecuteAsyncRetriesOnTransientAsync()
    {
        CosmosRetryPolicy policy = new();
        int attempts = 0;
        await policy.ExecuteAsync(() =>
        {
            attempts++;
            if (attempts < 2)
            {
                return Task.FromException<int>(CreateCosmosException(HttpStatusCode.TooManyRequests));
            }

            return Task.FromResult(42);
        });
        Assert.Equal(2, attempts);
    }
}