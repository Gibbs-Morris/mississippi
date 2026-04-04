using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;

using Mississippi.DomainModeling.Abstractions;


namespace MississippiSamples.Spring.L2Tests;

/// <summary>
///     Integration tests proving generated endpoint authorization behavior for the Spring auth-proof slice.
/// </summary>
[Collection(SpringTestCollection.Name)]
public sealed class AuthProofAuthorizationIntegrationTests
{
    private static readonly TimeSpan EventualConsistencyTimeout = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(500);

    private readonly SpringFixture fixture;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AuthProofAuthorizationIntegrationTests" /> class.
    /// </summary>
    /// <param name="fixture">The shared Spring fixture.</param>
    public AuthProofAuthorizationIntegrationTests(
        SpringFixture fixture
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

    private static Uri BuildSagaResumeUri(
        Guid sagaId
    ) =>
        new($"api/sagas/auth-proof/{sagaId}/resume", UriKind.Relative);

    private static Uri BuildSagaRuntimeStatusUri(
        Guid sagaId
    ) =>
        new($"api/sagas/auth-proof/{sagaId}/runtime-status", UriKind.Relative);

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

    private static async Task<HttpResponseMessage> GetAuthProofSagaRuntimeStatusAsync(
        HttpClient client,
        Guid sagaId,
        IReadOnlyDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default
    )
    {
        using HttpRequestMessage request = new(HttpMethod.Get, BuildSagaRuntimeStatusUri(sagaId));
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

    private static async Task<HttpResponseMessage> PostAuthProofSagaResumeAsync(
        HttpClient client,
        Guid sagaId,
        IReadOnlyDictionary<string, string>? headers = null
    )
    {
        using HttpRequestMessage request = new(HttpMethod.Post, BuildSagaResumeUri(sagaId))
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

    private static async Task<HttpStatusCode> WaitForSagaRuntimeStatusCodeAsync(
        HttpClient client,
        Guid sagaId,
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
                using HttpResponseMessage response = await GetAuthProofSagaRuntimeStatusAsync(
                    client,
                    sagaId,
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
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        using HttpClient client = fixture.CreateHttpClient();
        string aggregateId = $"auth-proof-{Guid.NewGuid():N}";
        using HttpResponseMessage response = await PostAuthProofCommandAsync(
            client,
            aggregateId,
            "authenticated",
            new Dictionary<string, string>
            {
                ["X-Spring-Anonymous"] = "true",
            });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    ///     Verifies claim-policy endpoint returns 200 when required claim is present.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task PolicyProtectedEndpointShouldReturn200WhenClaimPresent()
    {
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        using HttpClient client = fixture.CreateHttpClient();
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    ///     Verifies claim-policy endpoint returns 403 when required claim is missing.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task PolicyProtectedEndpointShouldReturn403WhenClaimMissing()
    {
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        using HttpClient client = fixture.CreateHttpClient();
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
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    ///     Verifies projection endpoint returns 200 when required claim is present.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ProjectionEndpointShouldReturn200WhenClaimPresent()
    {
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        using HttpClient client = fixture.CreateHttpClient();
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
            commandResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        HttpStatusCode projectionStatusCode = await WaitForProjectionStatusCodeAsync(
            client,
            aggregateId,
            authorizedHeaders,
            HttpStatusCode.OK);
        projectionStatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    ///     Verifies projection endpoint returns 401 when no identity is established.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ProjectionEndpointShouldReturn401ForAnonymousRequest()
    {
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        using HttpClient client = fixture.CreateHttpClient();
        string aggregateId = $"auth-proof-{Guid.NewGuid():N}";
        using HttpResponseMessage response = await GetAuthProofProjectionAsync(
            client,
            aggregateId,
            new Dictionary<string, string>
            {
                ["X-Spring-Anonymous"] = "true",
            });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    ///     Verifies projection endpoint returns 403 when required claim is missing.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ProjectionEndpointShouldReturn403WhenClaimMissing()
    {
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        using HttpClient client = fixture.CreateHttpClient();
        string aggregateId = $"auth-proof-{Guid.NewGuid():N}";
        using HttpResponseMessage response = await GetAuthProofProjectionAsync(
            client,
            aggregateId,
            new Dictionary<string, string>
            {
                ["X-Spring-User"] = "auth-proof-insufficient",
                ["X-Spring-Roles"] = "none",
            });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    ///     Verifies role-protected endpoint returns 200 when required role is present.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task RoleProtectedEndpointShouldReturn200WhenRolePresent()
    {
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        using HttpClient client = fixture.CreateHttpClient();
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
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    ///     Verifies role-protected endpoint returns 403 when authenticated identity lacks required role.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task RoleProtectedEndpointShouldReturn403WhenRoleMissing()
    {
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        using HttpClient client = fixture.CreateHttpClient();
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
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    ///     Verifies saga endpoint returns 200 when required role is present.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task SagaEndpointShouldReturn200WhenRolePresent()
    {
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        using HttpClient client = fixture.CreateHttpClient();
        Guid sagaId = Guid.NewGuid();
        using HttpResponseMessage response = await PostAuthProofSagaStartAsync(
            client,
            sagaId,
            new Dictionary<string, string>
            {
                ["X-Spring-User"] = "auth-proof-operator",
                ["X-Spring-Roles"] = "auth-proof-operator",
            });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    ///     Verifies saga endpoint returns 401 when no identity is established.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task SagaEndpointShouldReturn401ForAnonymousRequest()
    {
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        using HttpClient client = fixture.CreateHttpClient();
        Guid sagaId = Guid.NewGuid();
        using HttpResponseMessage response = await GetAuthProofSagaStatusAsync(
            client,
            sagaId,
            new Dictionary<string, string>
            {
                ["X-Spring-Anonymous"] = "true",
            });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    ///     Verifies saga endpoint returns 403 when authenticated identity lacks required role.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task SagaEndpointShouldReturn403WhenRoleMissing()
    {
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        using HttpClient client = fixture.CreateHttpClient();
        Guid sagaId = Guid.NewGuid();
        using HttpResponseMessage response = await GetAuthProofSagaStatusAsync(
            client,
            sagaId,
            new Dictionary<string, string>
            {
                ["X-Spring-User"] = "auth-proof-insufficient",
                ["X-Spring-Roles"] = "none",
            });
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    ///     Verifies manual resume returns an unauthorized disposition for a different authenticated caller once the start
    ///     fingerprint is captured.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task SagaResumeShouldReturnUnauthorizedDispositionForDifferentAuthenticatedCaller()
    {
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        using HttpClient client = fixture.CreateHttpClient();
        Guid sagaId = Guid.NewGuid();
        Dictionary<string, string> originatingHeaders = new()
        {
            ["X-Spring-User"] = "auth-proof-originator",
            ["X-Spring-Roles"] = "auth-proof-operator",
        };
        using (HttpResponseMessage startResponse =
               await PostAuthProofSagaStartAsync(client, sagaId, originatingHeaders))
        {
            startResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        HttpStatusCode runtimeStatusCode = await WaitForSagaRuntimeStatusCodeAsync(
            client,
            sagaId,
            originatingHeaders,
            HttpStatusCode.OK);
        runtimeStatusCode.Should().Be(HttpStatusCode.OK);
        using HttpResponseMessage response = await PostAuthProofSagaResumeAsync(
            client,
            sagaId,
            new Dictionary<string, string>
            {
                ["X-Spring-User"] = "auth-proof-different-user",
                ["X-Spring-Roles"] = "auth-proof-operator",
            });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SagaResumeResponse payload = (await response.Content.ReadFromJsonAsync<SagaResumeResponse>())!;
        payload.Disposition.Should().Be(SagaResumeRequestDisposition.Unauthorized);
        payload.Message.Should().Be("The current caller is not authorized for this saga.");
    }

    /// <summary>
    ///     Verifies runtime-status remains visible to the originating caller after the saga start fingerprint is propagated
    ///     into runtime recovery metadata.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task SagaRuntimeStatusShouldReturn200ForOriginatingCaller()
    {
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        using HttpClient client = fixture.CreateHttpClient();
        Guid sagaId = Guid.NewGuid();
        Dictionary<string, string> headers = new()
        {
            ["X-Spring-User"] = "auth-proof-originator",
            ["X-Spring-Roles"] = "auth-proof-operator",
        };
        using (HttpResponseMessage startResponse = await PostAuthProofSagaStartAsync(client, sagaId, headers))
        {
            startResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        HttpStatusCode runtimeStatusCode = await WaitForSagaRuntimeStatusCodeAsync(
            client,
            sagaId,
            headers,
            HttpStatusCode.OK);
        runtimeStatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    ///     Verifies runtime-status fails closed for a different authenticated caller once the saga access fingerprint is
    ///     captured.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task SagaRuntimeStatusShouldReturn404ForDifferentAuthenticatedCaller()
    {
        fixture.IsInitialized.Should().BeTrue("fixture must be initialized");
        using HttpClient client = fixture.CreateHttpClient();
        Guid sagaId = Guid.NewGuid();
        Dictionary<string, string> originatingHeaders = new()
        {
            ["X-Spring-User"] = "auth-proof-originator",
            ["X-Spring-Roles"] = "auth-proof-operator",
        };
        using (HttpResponseMessage startResponse =
               await PostAuthProofSagaStartAsync(client, sagaId, originatingHeaders))
        {
            startResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        HttpStatusCode runtimeStatusCode = await WaitForSagaRuntimeStatusCodeAsync(
            client,
            sagaId,
            originatingHeaders,
            HttpStatusCode.OK);
        runtimeStatusCode.Should().Be(HttpStatusCode.OK);
        using HttpResponseMessage response = await GetAuthProofSagaRuntimeStatusAsync(
            client,
            sagaId,
            new Dictionary<string, string>
            {
                ["X-Spring-User"] = "auth-proof-different-user",
                ["X-Spring-Roles"] = "auth-proof-operator",
            });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}