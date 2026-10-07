using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Mississippi.Inlet.Client.ActionEffects;
using Mississippi.Reservoir.Abstractions.Actions;

using MississippiSamples.Spring.Client.AuthSimulation;
using MississippiSamples.Spring.Client.Features.AuthProof.Dtos;
using MississippiSamples.Spring.Client.Features.AuthProofRead;
using MississippiSamples.Spring.Client.Features.AuthSimulation;


namespace MississippiSamples.Spring.Client.L0Tests.Features.AuthProofRead;

/// <summary>Protects current-persona observations from late, obsolete HTTP outcomes.</summary>
public sealed class AuthProofReadTests
{
    private static async Task<AuthProofProjectionReadCompletedAction> CompleteAsync(
        AuthProofReadEffect effect,
        ReadAuthProofProjectionAction request
    )
    {
        await using IAsyncEnumerator<IAction> results = effect
            .HandleAsync(request, new(), TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        Assert.True(await results.MoveNextAsync());
        AuthProofProjectionReadCompletedAction result =
            Assert.IsType<AuthProofProjectionReadCompletedAction>(results.Current);
        Assert.False(await results.MoveNextAsync());
        return result;
    }

    private sealed class ControlledProjectionFetcher : IProjectionFetcher
    {
        public int Calls { get; private set; }

        public string? EntityId { get; private set; }

        public Func<CancellationToken, Task<ProjectionFetchResult?>> Fetch { get; set; } =
            _ => Task.FromResult<ProjectionFetchResult?>(null);

        public Type? ProjectionType { get; private set; }

        public Task<ProjectionFetchResult?> FetchAsync(
            Type projectionType,
            string entityId,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            ProjectionType = projectionType;
            EntityId = entityId;
            return Fetch(cancellationToken);
        }
    }

    /// <summary>Caller cancellation after a read starts emits no timeout completion.</summary>
    /// <returns>The asynchronous caller-cancellation check.</returns>
    [Fact]
    public async Task CallerCancellationDoesNotEmitTimeoutCompletionAsync()
    {
        using AuthSimulationHeadersHandler headers = new();
        using CancellationTokenSource cancellation =
            CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        ControlledProjectionFetcher fetcher = new()
        {
            Fetch = async token =>
            {
                await cancellation.CancelAsync();
                return await Task.FromCanceled<ProjectionFetchResult?>(token);
            },
        };
        AuthProofReadEffect effect = new(fetcher, headers);
        ReadAuthProofProjectionAction request = new(Guid.NewGuid(), "cancelled-entity", new());
        await using IAsyncEnumerator<IAction> results = effect.HandleAsync(request, new(), cancellation.Token)
            .GetAsyncEnumerator(cancellation.Token);
        Assert.False(await results.MoveNextAsync());
        Assert.True(cancellation.IsCancellationRequested);
        Assert.Equal(1, fetcher.Calls);
        Assert.Equal(typeof(AuthProofProjectionDto), fetcher.ProjectionType);
        Assert.Equal(request.EntityId, fetcher.EntityId);
    }

    /// <summary>A profile mismatch is reported without sending a read under the wrong identity.</summary>
    /// <returns>The asynchronous persona-header check.</returns>
    [Fact]
    public async Task ChangedHttpPersonaDoesNotSendObsoleteReadAsync()
    {
        using AuthSimulationHeadersHandler headers = new();
        headers.SetProfile(true, null, null);
        ControlledProjectionFetcher fetcher = new();
        AuthProofReadEffect effect = new(fetcher, headers);
        AuthProofProjectionReadCompletedAction result = await CompleteAsync(
            effect,
            new(Guid.NewGuid(), "entity", new()));
        Assert.Equal(0, fetcher.Calls);
        Assert.Contains("persona changed", result.ErrorMessage, StringComparison.Ordinal);
        Assert.Null(result.Data);
    }

    /// <summary>An actual effect's delayed allowed result cannot replace a newer persona denial.</summary>
    /// <returns>The asynchronous out-of-order regression.</returns>
    [Fact]
    public async Task DelayedAllowedReadCannotReplaceNewPersonaDenialAsync()
    {
        using AuthSimulationHeadersHandler headers = new();
        using SemaphoreSlim release = new(0, 1);
        ControlledProjectionFetcher fetcher = new()
        {
            Fetch = async cancellationToken =>
            {
                await release.WaitAsync(cancellationToken);
                return ProjectionFetchResult.Create(new AuthProofProjectionDto(7), 3);
            },
        };
        AuthProofReadEffect effect = new(fetcher, headers);
        ReadAuthProofProjectionAction first = new(Guid.NewGuid(), "same-entity", new());
        AuthProofReadState state = AuthProofReadReducers.Request(new(), first);
        await using IAsyncEnumerator<IAction> firstResults = effect
            .HandleAsync(first, state, TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        Task<bool> firstPending = firstResults.MoveNextAsync().AsTask();
        Assert.Equal(1, fetcher.Calls);
        Assert.Equal(typeof(AuthProofProjectionDto), fetcher.ProjectionType);
        Assert.Equal("same-entity", fetcher.EntityId);
        Assert.False(firstPending.IsCompleted);
        headers.SetProfile(true, null, null);
        AuthSimulationState anonymous = new()
        {
            Name = "Unauthenticated",
            IsAnonymous = true,
            Roles = null,
            Claims = null,
        };
        ReadAuthProofProjectionAction latest = new(Guid.NewGuid(), first.EntityId, anonymous);
        state = AuthProofReadReducers.Request(state, latest);
        fetcher.Fetch = _ => throw new HttpRequestException("denied", null, HttpStatusCode.Unauthorized);
        AuthProofProjectionReadCompletedAction denial = await CompleteAsync(effect, latest);
        state = AuthProofReadReducers.Complete(state, denial);
        Assert.Contains("HTTP 401", state.ErrorMessage, StringComparison.Ordinal);
        AuthProofReadState deniedState = state;
        release.Release();
        Assert.True(await firstPending);
        state = AuthProofReadReducers.Complete(
            state,
            Assert.IsType<AuthProofProjectionReadCompletedAction>(firstResults.Current));
        Assert.False(await firstResults.MoveNextAsync());
        Assert.Same(deniedState, state);
        Assert.Null(state.Data);
        Assert.Equal(-1, state.Version);
        Assert.Equal("Unauthenticated", state.PersonaName);
        Assert.Contains("HTTP 401", state.ErrorMessage, StringComparison.Ordinal);
    }

    /// <summary>Forbidden reads remain explicit claim-policy failures.</summary>
    /// <returns>The asynchronous forbidden-read check.</returns>
    [Fact]
    public async Task ForbiddenReadReportsClaimPolicyAsync()
    {
        using AuthSimulationHeadersHandler headers = new();
        ControlledProjectionFetcher fetcher = new()
        {
            Fetch = _ => throw new HttpRequestException("denied", null, HttpStatusCode.Forbidden),
        };
        AuthProofReadEffect effect = new(fetcher, headers);
        AuthProofProjectionReadCompletedAction result = await CompleteAsync(
            effect,
            new(Guid.NewGuid(), "entity", new()));
        Assert.Contains("HTTP 403", result.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains("claim policy", result.ErrorMessage, StringComparison.Ordinal);
        Assert.Null(result.Data);
    }

    /// <summary>Earlier data, empty results and errors cannot replace a different entity's observation.</summary>
    /// <param name="outcome">The obsolete outcome kind.</param>
    [Theory]
    [InlineData("data")]
    [InlineData("empty")]
    [InlineData("error")]
    public void LateOutcomeCannotReplaceNewEntity(
        string outcome
    )
    {
        Guid oldRequest = Guid.NewGuid();
        ReadAuthProofProjectionAction latest = new(Guid.NewGuid(), "second-entity", new());
        AuthProofReadState pending = AuthProofReadReducers.Request(new(), latest);
        AuthProofProjectionReadCompletedAction obsolete = new(
            oldRequest,
            outcome == "data" ? new(99) : null,
            20,
            outcome == "error" ? "old denial" : null);
        Assert.Same(pending, AuthProofReadReducers.Complete(pending, obsolete));
        Assert.True(pending.IsLoading);
        Assert.Null(pending.Data);
        Assert.Null(pending.ErrorMessage);
        Assert.Equal("second-entity", pending.EntityId);
        AuthProofReadState completed = AuthProofReadReducers.Complete(pending, new(latest.RequestId, new(2), 1, null));
        Assert.Same(completed, AuthProofReadReducers.Complete(completed, obsolete));
        Assert.Equal(2, completed.Data?.AuthenticatedAccessCount);
        Assert.Equal(1, completed.Version);
        Assert.False(completed.IsLoading);
        Assert.Null(completed.ErrorMessage);
    }

    /// <summary>A malformed projection response completes as a correlated read failure without data.</summary>
    /// <returns>The asynchronous malformed-response check.</returns>
    [Fact]
    public async Task MalformedProjectionResponseReportsReadFailureAsync()
    {
        using AuthSimulationHeadersHandler headers = new();
        ControlledProjectionFetcher fetcher = new()
        {
            Fetch = _ => throw new JsonException("invalid projection JSON"),
        };
        AuthProofReadEffect effect = new(fetcher, headers);
        ReadAuthProofProjectionAction request = new(Guid.NewGuid(), "malformed-entity", new());
        AuthProofProjectionReadCompletedAction result = await CompleteAsync(effect, request);
        Assert.Equal(1, fetcher.Calls);
        Assert.Equal(typeof(AuthProofProjectionDto), fetcher.ProjectionType);
        Assert.Equal(request.EntityId, fetcher.EntityId);
        Assert.Equal(request.RequestId, result.RequestId);
        Assert.Null(result.Data);
        Assert.Equal(-1, result.Version);
        Assert.Equal("The projection response could not be read: invalid projection JSON", result.ErrorMessage);
        AuthProofReadState state = AuthProofReadReducers.Complete(
            AuthProofReadReducers.Request(new(), request),
            result);
        Assert.False(state.IsLoading);
        Assert.Null(state.Data);
        Assert.Equal(-1, state.Version);
        Assert.Equal(result.ErrorMessage, state.ErrorMessage);
        Assert.Equal(request.EntityId, state.EntityId);
        Assert.Equal("Full Access", state.PersonaName);
    }

    /// <summary>A real not-found sentinel is an allowed empty read, not a permission error.</summary>
    /// <returns>The asynchronous empty-read check.</returns>
    [Fact]
    public async Task NotFoundIsAnEmptyAllowedObservationAsync()
    {
        using AuthSimulationHeadersHandler headers = new();
        ControlledProjectionFetcher fetcher = new()
        {
            Fetch = _ => Task.FromResult<ProjectionFetchResult?>(ProjectionFetchResult.NotFound),
        };
        AuthProofReadEffect effect = new(fetcher, headers);
        ReadAuthProofProjectionAction request = new(Guid.NewGuid(), "empty-entity", new());
        AuthProofReadState state = AuthProofReadReducers.Complete(
            AuthProofReadReducers.Request(new(), request),
            await CompleteAsync(effect, request));
        Assert.Null(state.Data);
        Assert.Null(state.ErrorMessage);
        Assert.False(state.IsLoading);
        Assert.Equal(0, state.Version);
        Assert.Equal("Full Access", state.PersonaName);
    }

    /// <summary>Unavailable HTTP and transport failures settle as correlated read errors.</summary>
    /// <param name="statusCode">The HTTP status, or null for a transport failure.</param>
    /// <returns>The asynchronous general-read-failure check.</returns>
    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(null)]
    public async Task OtherHttpFailuresReportReadFailureAsync(
        HttpStatusCode? statusCode
    )
    {
        using AuthSimulationHeadersHandler headers = new();
        ControlledProjectionFetcher fetcher = new()
        {
            Fetch = _ => throw new HttpRequestException("projection unavailable", null, statusCode),
        };
        AuthProofReadEffect effect = new(fetcher, headers);
        ReadAuthProofProjectionAction request = new(Guid.NewGuid(), "unavailable-entity", new());
        AuthProofProjectionReadCompletedAction result = await CompleteAsync(effect, request);
        Assert.Equal(1, fetcher.Calls);
        Assert.Equal(request.RequestId, result.RequestId);
        Assert.Null(result.Data);
        Assert.Equal(-1, result.Version);
        Assert.Equal("Protected read failed: projection unavailable", result.ErrorMessage);
        AuthProofReadState state = AuthProofReadReducers.Complete(
            AuthProofReadReducers.Request(new(), request),
            result);
        Assert.False(state.IsLoading);
        Assert.Null(state.Data);
        Assert.Equal(-1, state.Version);
        Assert.Equal(result.ErrorMessage, state.ErrorMessage);
        Assert.Equal(request.EntityId, state.EntityId);
    }

    /// <summary>Changing persona invalidates displayed data and outstanding outcomes immediately.</summary>
    [Fact]
    public void PersonaChangeInvalidatesOutstandingRead()
    {
        ReadAuthProofProjectionAction request = new(Guid.NewGuid(), "first-entity", new());
        AuthProofReadState pending = AuthProofReadReducers.Request(new(), request);
        AuthProofReadState invalidated = AuthProofReadReducers.InvalidatePersona(
            pending,
            AuthSimulationProfiles.Unauthenticated);
        Assert.Null(invalidated.RequestId);
        Assert.Null(invalidated.Data);
        Assert.Null(invalidated.PersonaName);
        Assert.False(invalidated.IsLoading);
        Assert.Same(invalidated, AuthProofReadReducers.Complete(invalidated, new(request.RequestId, new(8), 4, null)));
    }

    /// <summary>A transport timeout with an active caller token completes with refresh guidance.</summary>
    /// <returns>The asynchronous timeout check.</returns>
    [Fact]
    public async Task TransportTimeoutCompletesWithRefreshGuidanceAsync()
    {
        Assert.False(TestContext.Current.CancellationToken.IsCancellationRequested);
        using AuthSimulationHeadersHandler headers = new();
        ControlledProjectionFetcher fetcher = new()
        {
            Fetch = async _ =>
                await Task.FromException<ProjectionFetchResult?>(new TaskCanceledException("transport timeout")),
        };
        AuthProofReadEffect effect = new(fetcher, headers);
        ReadAuthProofProjectionAction request = new(Guid.NewGuid(), "timed-out-entity", new());
        AuthProofProjectionReadCompletedAction result = await CompleteAsync(effect, request);
        Assert.False(TestContext.Current.CancellationToken.IsCancellationRequested);
        Assert.Equal(1, fetcher.Calls);
        Assert.Equal(request.RequestId, result.RequestId);
        Assert.Null(result.Data);
        Assert.Equal(-1, result.Version);
        Assert.Equal("The protected read timed out. Refresh to try again.", result.ErrorMessage);
        AuthProofReadState state = AuthProofReadReducers.Complete(
            AuthProofReadReducers.Request(new(), request),
            result);
        Assert.False(state.IsLoading);
        Assert.Null(state.Data);
        Assert.Equal(-1, state.Version);
        Assert.Equal(result.ErrorMessage, state.ErrorMessage);
    }

    /// <summary>Missing DTO registration cannot masquerade as an allowed empty projection.</summary>
    /// <returns>The asynchronous missing-fetcher check.</returns>
    [Fact]
    public async Task UnsupportedProjectionReportsFailureAsync()
    {
        using AuthSimulationHeadersHandler headers = new();
        ControlledProjectionFetcher fetcher = new();
        AuthProofReadEffect effect = new(fetcher, headers);
        AuthProofProjectionReadCompletedAction result = await CompleteAsync(
            effect,
            new(Guid.NewGuid(), "entity", new()));
        Assert.Contains("No projection fetcher", result.ErrorMessage, StringComparison.Ordinal);
        Assert.Null(result.Data);
    }
}