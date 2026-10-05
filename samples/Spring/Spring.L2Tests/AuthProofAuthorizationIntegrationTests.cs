using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;


namespace MississippiSamples.Spring.L2Tests;

/// <summary>
///     Integration tests proving generated endpoint authorization behavior for the Spring auth-proof slice.
/// </summary>
[Collection(SpringApiCollectionDefinition.Name)]
public sealed class AuthProofAuthorizationIntegrationTests
{
    private static readonly TimeSpan EventualConsistencyTimeout = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(500);

    private readonly SpringApplicationFixture fixture;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AuthProofAuthorizationIntegrationTests" /> class.
    /// </summary>
    /// <param name="fixture">The shared Spring fixture.</param>
    public AuthProofAuthorizationIntegrationTests(
        SpringApplicationFixture fixture
    ) =>
        this.fixture = fixture;

    private static Uri BuildCommandUri(
        string aggregateId,
        string route
    ) =>
        new($"api/aggregates/auth-proof/{aggregateId}/{route}", UriKind.Relative);

    private static Uri BuildProjectionUri(
        string aggregateId
    ) =>
        new($"api/projections/auth-proof/{aggregateId}", UriKind.Relative);

    private static Uri BuildSagaStatusUri(
        Guid sagaId
    ) =>
        new($"api/sagas/auth-proof/{sagaId}/status", UriKind.Relative);

    private static async Task<HttpResponseMessage> GetAuthProofProjectionAsync(
        HttpClient client,
        string aggregateId,
        IReadOnlyDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default
    )
    {
        using HttpRequestMessage request = new(HttpMethod.Get, BuildProjectionUri(aggregateId));
        if (headers is not null)
        {
            foreach ((string key, string value) in headers)
            {
                request.Headers.Remove(key);
                request.Headers.Add(key, value);
            }
        }

        return await client.SendAsync(request, cancellationToken);
    }

    private static async Task<HttpResponseMessage> GetAuthProofSagaStatusAsync(
        HttpClient client,
        Guid sagaId,
        IReadOnlyDictionary<string, string>? headers = null
    )
    {
        using HttpRequestMessage request = new(HttpMethod.Get, BuildSagaStatusUri(sagaId));
        if (headers is not null)
        {
            foreach ((string key, string value) in headers)
            {
                request.Headers.Remove(key);
                request.Headers.Add(key, value);
            }
        }

        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostAuthProofCommandAsync(
        HttpClient client,
        string aggregateId,
        string route,
        IReadOnlyDictionary<string, string>? headers = null
    )
    {
        using HttpRequestMessage request = new(HttpMethod.Post, BuildCommandUri(aggregateId, route))
        {
            Content = JsonContent.Create(
                new
                {
                }),
        };
        if (headers is not null)
        {
            foreach ((string key, string value) in headers)
            {
                request.Headers.Remove(key);
                request.Headers.Add(key, value);
            }
        }

        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostAuthProofSagaStartAsync(
        HttpClient client,
        Guid sagaId,
        IReadOnlyDictionary<string, string>? headers = null
    )
    {
        using HttpRequestMessage request =
            new(HttpMethod.Post, new Uri($"api/sagas/auth-proof/{sagaId}", UriKind.Relative))
            {
                Content = JsonContent.Create(
                    new
                    {
                        Marker = "auth-proof",
                    }),
            };
        if (headers is not null)
        {
            foreach ((string key, string value) in headers)
            {
                request.Headers.Remove(key);
                request.Headers.Add(key, value);
            }
        }

        return await client.SendAsync(request);
    }

    private static async Task<HttpStatusCode> WaitForProjectionStatusCodeAsync(
        HttpClient client,
        string aggregateId,
        IReadOnlyDictionary<string, string> headers,
        HttpStatusCode expectedStatusCode,
        CancellationToken cancellationToken = default
    )
    {
        using CancellationTokenSource timeoutSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(EventualConsistencyTimeout);
        CancellationToken timeoutToken = timeoutSource.Token;
        HttpStatusCode lastStatusCode = HttpStatusCode.NotFound;
        try
        {
            while (!timeoutToken.IsCancellationRequested)
            {
                using HttpResponseMessage response = await GetAuthProofProjectionAsync(
                    client,
                    aggregateId,
                    headers,
                    timeoutToken);
                lastStatusCode = response.StatusCode;
                if (response.StatusCode == expectedStatusCode)
                {
                    return response.StatusCode;
                }

                await Task.Delay(PollingInterval, timeoutToken);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return lastStatusCode;
        }

        return lastStatusCode;
    }

    /// <summary>
    ///     Verifies authenticated-only endpoint returns 401 when no identity is established.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task AuthenticatedEndpointShouldReturn401ForAnonymousRequest()
    {
        Assert.True(fixture.IsInitialized, "fixture must be initialized");
        HttpClient client = fixture.GatewayClient;
        string aggregateId = $"auth-proof-{Guid.NewGuid():N}";
        using HttpResponseMessage response = await PostAuthProofCommandAsync(
            client,
            aggregateId,
            "authenticated",
            new Dictionary<string, string>
            {
                ["X-Spring-Anonymous"] = "true",
            });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    ///     Verifies claim-policy endpoint returns 200 when required claim is present.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task PolicyProtectedEndpointShouldReturn200WhenClaimPresent()
    {
        Assert.True(fixture.IsInitialized, "fixture must be initialized");
        HttpClient client = fixture.GatewayClient;
        string aggregateId = $"auth-proof-{Guid.NewGuid():N}";
        using HttpResponseMessage response = await PostAuthProofCommandAsync(
            client,
            aggregateId,
            "policy",
            new Dictionary<string, string>
            {
                ["X-Spring-User"] = "auth-proof-policy-user",
                ["X-Spring-Roles"] = "none",
                ["X-Spring-Claims"] = "spring.permission=auth-proof",
            });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    ///     Verifies claim-policy endpoint returns 403 when required claim is missing.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task PolicyProtectedEndpointShouldReturn403WhenClaimMissing()
    {
        Assert.True(fixture.IsInitialized, "fixture must be initialized");
        HttpClient client = fixture.GatewayClient;
        string aggregateId = $"auth-proof-{Guid.NewGuid():N}";
        using HttpResponseMessage response = await PostAuthProofCommandAsync(
            client,
            aggregateId,
            "policy",
            new Dictionary<string, string>
            {
                ["X-Spring-User"] = "auth-proof-insufficient",
                ["X-Spring-Roles"] = "none",
            });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    ///     Verifies projection endpoint returns 200 when required claim is present.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ProjectionEndpointShouldReturn200WhenClaimPresent()
    {
        Assert.True(fixture.IsInitialized, "fixture must be initialized");
        HttpClient client = fixture.GatewayClient;
        string aggregateId = $"auth-proof-{Guid.NewGuid():N}";
        Dictionary<string, string> authorizedHeaders = new()
        {
            ["X-Spring-User"] = "auth-proof-claim-user",
            ["X-Spring-Roles"] = "none",
            ["X-Spring-Claims"] = "spring.permission=auth-proof",
        };
        using (HttpResponseMessage commandResponse = await PostAuthProofCommandAsync(
                   client,
                   aggregateId,
                   "authenticated",
                   authorizedHeaders))
        {
            Assert.Equal(HttpStatusCode.OK, commandResponse.StatusCode);
        }

        HttpStatusCode projectionStatusCode = await WaitForProjectionStatusCodeAsync(
            client,
            aggregateId,
            authorizedHeaders,
            HttpStatusCode.OK,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, projectionStatusCode);
    }

    /// <summary>
    ///     Verifies projection endpoint returns 401 when no identity is established.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ProjectionEndpointShouldReturn401ForAnonymousRequest()
    {
        Assert.True(fixture.IsInitialized, "fixture must be initialized");
        HttpClient client = fixture.GatewayClient;
        string aggregateId = $"auth-proof-{Guid.NewGuid():N}";
        using HttpResponseMessage response = await GetAuthProofProjectionAsync(
            client,
            aggregateId,
            new Dictionary<string, string>
            {
                ["X-Spring-Anonymous"] = "true",
            },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    ///     Verifies projection endpoint returns 403 when required claim is missing.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ProjectionEndpointShouldReturn403WhenClaimMissing()
    {
        Assert.True(fixture.IsInitialized, "fixture must be initialized");
        HttpClient client = fixture.GatewayClient;
        string aggregateId = $"auth-proof-{Guid.NewGuid():N}";
        using HttpResponseMessage response = await GetAuthProofProjectionAsync(
            client,
            aggregateId,
            new Dictionary<string, string>
            {
                ["X-Spring-User"] = "auth-proof-insufficient",
                ["X-Spring-Roles"] = "none",
            },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    ///     Verifies role-protected endpoint returns 200 when required role is present.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task RoleProtectedEndpointShouldReturn200WhenRolePresent()
    {
        Assert.True(fixture.IsInitialized, "fixture must be initialized");
        HttpClient client = fixture.GatewayClient;
        string aggregateId = $"auth-proof-{Guid.NewGuid():N}";
        using HttpResponseMessage response = await PostAuthProofCommandAsync(
            client,
            aggregateId,
            "role",
            new Dictionary<string, string>
            {
                ["X-Spring-User"] = "auth-proof-operator",
                ["X-Spring-Roles"] = "auth-proof-operator",
            });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    ///     Verifies role-protected endpoint returns 403 when authenticated identity lacks required role.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task RoleProtectedEndpointShouldReturn403WhenRoleMissing()
    {
        Assert.True(fixture.IsInitialized, "fixture must be initialized");
        HttpClient client = fixture.GatewayClient;
        string aggregateId = $"auth-proof-{Guid.NewGuid():N}";
        using HttpResponseMessage response = await PostAuthProofCommandAsync(
            client,
            aggregateId,
            "role",
            new Dictionary<string, string>
            {
                ["X-Spring-User"] = "auth-proof-insufficient",
                ["X-Spring-Roles"] = "none",
            });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    ///     Verifies saga endpoint returns 200 when required role is present.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task SagaEndpointShouldReturn200WhenRolePresent()
    {
        Assert.True(fixture.IsInitialized, "fixture must be initialized");
        HttpClient client = fixture.GatewayClient;
        Guid sagaId = Guid.NewGuid();
        using HttpResponseMessage response = await PostAuthProofSagaStartAsync(
            client,
            sagaId,
            new Dictionary<string, string>
            {
                ["X-Spring-User"] = "auth-proof-operator",
                ["X-Spring-Roles"] = "auth-proof-operator",
            });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    ///     Verifies saga endpoint returns 401 when no identity is established.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task SagaEndpointShouldReturn401ForAnonymousRequest()
    {
        Assert.True(fixture.IsInitialized, "fixture must be initialized");
        HttpClient client = fixture.GatewayClient;
        Guid sagaId = Guid.NewGuid();
        using HttpResponseMessage response = await GetAuthProofSagaStatusAsync(
            client,
            sagaId,
            new Dictionary<string, string>
            {
                ["X-Spring-Anonymous"] = "true",
            });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    ///     Verifies saga endpoint returns 403 when authenticated identity lacks required role.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task SagaEndpointShouldReturn403WhenRoleMissing()
    {
        Assert.True(fixture.IsInitialized, "fixture must be initialized");
        HttpClient client = fixture.GatewayClient;
        Guid sagaId = Guid.NewGuid();
        using HttpResponseMessage response = await GetAuthProofSagaStatusAsync(
            client,
            sagaId,
            new Dictionary<string, string>
            {
                ["X-Spring-User"] = "auth-proof-insufficient",
                ["X-Spring-Roles"] = "none",
            });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}