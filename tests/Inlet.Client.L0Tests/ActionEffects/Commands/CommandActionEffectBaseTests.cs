using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Inlet.Client.L0Tests.ActionEffects.CommandUrls;
using Mississippi.Reservoir.Abstractions.Actions;

using Moq;


namespace MississippiTests.Inlet.Client.L0Tests.ActionEffects.Commands;

/// <summary>
///     Verifies the shared aggregate command transport and lifecycle contract.
/// </summary>
public sealed class CommandActionEffectBaseTests
{
    private static readonly string[] LogFieldNames = ["CommandId", "CommandType", "StatusCode", "{OriginalFormat}"];

    private static readonly DateTimeOffset StartedAt = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    private static CommandEffectFailedAction AssertFailure(
        List<IAction> actions,
        string errorCode,
        string? errorMessage = null
    )
    {
        Assert.Equal(2, actions.Count);
        CommandEffectExecutingAction executing = Assert.IsType<CommandEffectExecutingAction>(actions[0]);
        CommandEffectFailedAction failed = Assert.IsType<CommandEffectFailedAction>(actions[1]);
        Assert.Equal(nameof(CommandEffectAction), executing.CommandType);
        Assert.True(Guid.TryParseExact(executing.CommandId, "N", out Guid _));
        Assert.Equal(executing.CommandId, failed.CommandId);
        Assert.Equal(StartedAt, executing.Timestamp);
        Assert.Equal(StartedAt.AddSeconds(2), failed.Timestamp);
        Assert.Equal(errorCode, failed.ErrorCode);
        Assert.NotNull(failed.ErrorMessage);
        if (errorMessage is not null)
        {
            Assert.Equal(errorMessage, failed.ErrorMessage);
        }

        return failed;
    }

    private static async Task<List<IAction>> CollectAsync(
        CommandEffect effect,
        IAction action,
        CancellationToken cancellationToken
    )
    {
        List<IAction> actions = [];
        await foreach (IAction emitted in effect.HandleAsync(action, new(), cancellationToken))
        {
            actions.Add(emitted);
        }

        return actions;
    }

    private static HttpClient CreateClient(
        CommandEffectHttpHandler handler
    ) =>
        new(handler)
        {
            BaseAddress = new("https://commands.test"),
        };

    private static Mock<IMapper<CommandEffectAction, Dictionary<string, string>>> CreateMapper(
        CommandEffectAction action
    )
    {
        Mock<IMapper<CommandEffectAction, Dictionary<string, string>>> mapper = new(MockBehavior.Strict);
        mapper.Setup(value => value.Map(action))
            .Returns(
                new Dictionary<string, string>
                {
                    ["value"] = "mapped",
                });
        return mapper;
    }

    private static HttpResponseMessage CreateResponse(
        HttpStatusCode status,
        string body
    ) =>
        new(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

    private static async Task<List<IAction>> ExecuteResponseAsync(
        HttpStatusCode status,
        string body,
        CancellationToken cancellationToken
    )
    {
        CommandEffectAction action = new("entity-1");
        FakeTimeProvider clock = new(StartedAt);
        using CommandEffectHttpHandler handler = new((_, _) =>
        {
            clock.Advance(TimeSpan.FromSeconds(2));
            return Task.FromResult(CreateResponse(status, body));
        });
        using HttpClient http = CreateClient(handler);
        CommandEffect effect = new(http, CreateMapper(action).Object, clock);
        return await CollectAsync(effect, action, cancellationToken);
    }

    /// <summary>
    ///     Only the configured command action is handled.
    /// </summary>
    [Fact]
    public void CanHandleSelectsOnlyTheCommandType()
    {
        using HttpClient http = new();
        Mock<IMapper<CommandEffectAction, Dictionary<string, string>>> mapper = new(MockBehavior.Strict);
        CommandEffect effect = new(http, mapper.Object);
        Assert.True(effect.CanHandle(new CommandEffectAction("entity-1")));
        Assert.False(effect.CanHandle(new CommandEffectSucceededAction("other", StartedAt)));
    }

    /// <summary>
    ///     Omitting the clock uses the system provider without reading wall-clock time.
    /// </summary>
    [Fact]
    public void ConstructorDefaultsToSystemTimeProvider()
    {
        using HttpClient http = new();
        Mock<IMapper<CommandEffectAction, Dictionary<string, string>>> mapper = new(MockBehavior.Strict);
        CommandEffect effect = new(http, mapper.Object);
        Assert.Same(TimeProvider.System, effect.Clock);
    }

    /// <summary>
    ///     The HTTP dependency is required.
    /// </summary>
    [Fact]
    public void ConstructorRejectsNullHttpClient()
    {
        Mock<IMapper<CommandEffectAction, Dictionary<string, string>>> mapper = new(MockBehavior.Strict);
        ArgumentNullException error =
            Assert.Throws<ArgumentNullException>(() => new CommandEffect(null!, mapper.Object));
        Assert.Equal("httpClient", error.ParamName);
    }

    /// <summary>
    ///     The mapping dependency is required.
    /// </summary>
    [Fact]
    public void ConstructorRejectsNullMapper()
    {
        using HttpClient http = new();
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => new CommandEffect(http, null!));
        Assert.Equal("mapper", error.ParamName);
    }

    /// <summary>
    ///     Network and timeout errors terminate the correlated lifecycle.
    /// </summary>
    /// <param name="isTimeout">Whether to simulate a timeout instead of a network failure.</param>
    /// <param name="errorMessage">The expected failure description.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Theory]
    [InlineData(false, "Network error: connection failed")]
    [InlineData(true, "Request cancelled: request timed out")]
    public async Task HandleAsyncConvertsTransportExceptionsToFailuresAsync(
        bool isTimeout,
        string errorMessage
    )
    {
        CommandEffectAction action = new("entity-1");
        FakeTimeProvider clock = new(StartedAt);
        using CommandEffectHttpHandler handler = new((_, _) =>
        {
            clock.Advance(TimeSpan.FromSeconds(2));
            throw isTimeout
                ? new TaskCanceledException("request timed out")
                : new HttpRequestException("connection failed");
        });
        using HttpClient http = CreateClient(handler);
        CommandEffect effect = new(http, CreateMapper(action).Object, clock);
        List<IAction> actions = await CollectAsync(effect, action, TestContext.Current.CancellationToken);
        AssertFailure(actions, "HttpError", errorMessage);
    }

    /// <summary>
    ///     Domain failures preserve supplied details and use fallbacks only for missing values.
    /// </summary>
    /// <param name="body">The server response.</param>
    /// <param name="errorCode">The expected failure category.</param>
    /// <param name="errorMessage">The expected failure description.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Theory]
    [InlineData(
        "{\"success\":false,\"errorCode\":\"Conflict\",\"errorMessage\":\"Already exists\"}",
        "Conflict",
        "Already exists")]
    [InlineData("{\"success\":false,\"errorCode\":null,\"errorMessage\":\"Rejected\"}", "Unknown", "Rejected")]
    [InlineData("{\"success\":false,\"errorCode\":\"Invalid\",\"errorMessage\":null}", "Invalid", "Unknown error")]
    [InlineData("{\"success\":false}", "Unknown", "Unknown error")]
    public async Task HandleAsyncEmitsCorrelatedDomainFailureAsync(
        string body,
        string errorCode,
        string errorMessage
    )
    {
        List<IAction> actions = await ExecuteResponseAsync(
            HttpStatusCode.OK,
            body,
            TestContext.Current.CancellationToken);
        AssertFailure(actions, errorCode, errorMessage);
    }

    /// <summary>
    ///     An unsupported response charset terminates the command with a correlated response failure.
    /// </summary>
    /// <param name="status">The server response status.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Theory]
    [InlineData(HttpStatusCode.OK)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task HandleAsyncEmitsFailureForInvalidResponseCharsetAsync(
        HttpStatusCode status
    )
    {
        CommandEffectAction action = new("entity-1");
        FakeTimeProvider clock = new(StartedAt);
        using CommandEffectHttpHandler handler = new((_, _) =>
        {
            clock.Advance(TimeSpan.FromSeconds(2));
            HttpResponseMessage response = CreateResponse(status, "{\"success\":true}");
            response.Content.Headers.ContentType!.CharSet = "invalid-charset";
            return Task.FromResult(response);
        });
        using HttpClient http = CreateClient(handler);
        CommandEffect effect = new(http, CreateMapper(action).Object, clock);
        List<IAction> actions = await CollectAsync(effect, action, TestContext.Current.CancellationToken);
        CommandEffectFailedAction failed = AssertFailure(actions, "HttpError");
        Assert.StartsWith("Invalid response: ", failed.ErrorMessage, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Invalid successful responses terminate the command rather than abandoning its executing state.
    /// </summary>
    /// <param name="body">The invalid server response.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Theory]
    [InlineData("not JSON")]
    [InlineData("")]
    [InlineData("{\"success\":\"invalid\"}")]
    public async Task HandleAsyncEmitsFailureForMalformedResponseAsync(
        string body
    )
    {
        List<IAction> actions = await ExecuteResponseAsync(
            HttpStatusCode.OK,
            body,
            TestContext.Current.CancellationToken);
        CommandEffectFailedAction failed = AssertFailure(actions, "HttpError");
        Assert.StartsWith("Invalid response: ", failed.ErrorMessage, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Non-success HTTP statuses include the status and response body without attempting JSON parsing.
    /// </summary>
    /// <param name="status">The HTTP response status.</param>
    /// <param name="body">The response content.</param>
    /// <param name="errorMessage">The expected failure description.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "invalid command", "Server error (400): invalid command")]
    [InlineData(HttpStatusCode.InternalServerError, "server unavailable", "Server error (500): server unavailable")]
    public async Task HandleAsyncEmitsHttpFailureAsync(
        HttpStatusCode status,
        string body,
        string errorMessage
    )
    {
        List<IAction> actions = await ExecuteResponseAsync(status, body, TestContext.Current.CancellationToken);
        AssertFailure(actions, "HttpError", errorMessage);
    }

    /// <summary>
    ///     A JSON null response terminates the command with the documented no-response failure.
    /// </summary>
    /// <returns>The asynchronous test operation.</returns>
    [Fact]
    public async Task HandleAsyncEmitsNoResponseFailureAsync()
    {
        List<IAction> actions = await ExecuteResponseAsync(
            HttpStatusCode.OK,
            "null",
            TestContext.Current.CancellationToken);
        AssertFailure(actions, "NoResponse", "No response from server.");
    }

    /// <summary>
    ///     Caller cancellation reaches the HTTP handler and produces a terminal failure.
    /// </summary>
    /// <returns>The asynchronous test operation.</returns>
    [Fact]
    public async Task HandleAsyncForwardsCallerCancellationAsync()
    {
        CommandEffectAction action = new("entity-1");
        FakeTimeProvider clock = new(StartedAt);
        using CancellationTokenSource cancellation =
            CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        using CommandEffectHttpHandler handler = new(async (_, token) =>
        {
            Assert.True(token.CanBeCanceled);
            await cancellation.CancelAsync();
            Assert.True(token.IsCancellationRequested);
            clock.Advance(TimeSpan.FromSeconds(2));
            return await Task.FromCanceled<HttpResponseMessage>(token);
        });
        using HttpClient http = CreateClient(handler);
        CommandEffect effect = new(http, CreateMapper(action).Object, clock);
        List<IAction> actions = await CollectAsync(effect, action, cancellation.Token);
        Assert.True(cancellation.IsCancellationRequested);
        CommandEffectFailedAction failed = AssertFailure(actions, "HttpError");
        Assert.StartsWith("Request cancelled: ", failed.ErrorMessage, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Unsupported actions produce no lifecycle actions or HTTP requests.
    /// </summary>
    /// <returns>The asynchronous test operation.</returns>
    [Fact]
    public async Task HandleAsyncIgnoresUnsupportedActionsAsync()
    {
        using CommandEffectHttpHandler handler = new((_, _) =>
            throw new InvalidOperationException("Unexpected HTTP request."));
        using HttpClient http = new(handler);
        Mock<IMapper<CommandEffectAction, Dictionary<string, string>>> mapper = new(MockBehavior.Strict);
        CommandEffect effect = new(http, mapper.Object, new FakeTimeProvider(StartedAt));
        List<IAction> actions = await CollectAsync(
            effect,
            new CommandEffectSucceededAction("other", StartedAt),
            TestContext.Current.CancellationToken);
        Assert.Empty(actions);
        mapper.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Response diagnostics retain the original exception and safe context without changing failure actions.
    /// </summary>
    /// <param name="isJson">Whether to simulate JSON decoding instead of charset decoding.</param>
    /// <param name="status">The HTTP response status.</param>
    /// <param name="isEnabled">Whether the diagnostic level is enabled.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Theory]
    [InlineData(true, HttpStatusCode.OK, true)]
    [InlineData(false, HttpStatusCode.OK, true)]
    [InlineData(false, HttpStatusCode.BadRequest, true)]
    [InlineData(true, HttpStatusCode.OK, false)]
    [InlineData(false, HttpStatusCode.OK, false)]
    [InlineData(false, HttpStatusCode.BadRequest, false)]
    public async Task HandleAsyncLogsOriginalResponseExceptionWithSafeContextAsync(
        bool isJson,
        HttpStatusCode status,
        bool isEnabled
    )
    {
        const string format =
            "Command {CommandId} ({CommandType}) received an invalid response with HTTP status {StatusCode}.";
        CommandEffectAction action = new("sensitive-entity");
        FakeTimeProvider clock = new(StartedAt);
        Mock<ILogger<CommandEffectAction>> logger = new();
        logger.Setup(value => value.IsEnabled(LogLevel.Warning)).Returns(isEnabled);
        using CommandEffectHttpHandler handler = new((_, _) =>
        {
            clock.Advance(TimeSpan.FromSeconds(2));
            HttpResponseMessage response = CreateResponse(status, "{\"success\":\"invalid\"}");
            if (!isJson)
            {
                response.Content.Headers.ContentType!.CharSet = "invalid-charset";
            }

            return Task.FromResult(response);
        });
        using HttpClient http = CreateClient(handler);
        CommandEffect effect = new(http, CreateMapper(action).Object, clock, logger.Object);
        List<IAction> actions = await CollectAsync(effect, action, TestContext.Current.CancellationToken);
        CommandEffectFailedAction failed = AssertFailure(actions, "HttpError");
        if (!isEnabled)
        {
            Assert.DoesNotContain(logger.Invocations, invocation => invocation.Method.Name == nameof(ILogger.Log));
            return;
        }

        IInvocation log = Assert.Single(
            logger.Invocations,
            invocation => invocation.Method.Name == nameof(ILogger.Log));
        Assert.Equal(LogLevel.Warning, Assert.IsType<LogLevel>(log.Arguments[0]));
        EventId eventId = Assert.IsType<EventId>(log.Arguments[1]);
        Assert.Equal(1, eventId.Id);
        Assert.Equal("InvalidCommandResponse", eventId.Name);
        Exception logged = Assert.IsType<Exception>(log.Arguments[3], false);
        Assert.Equal($"Invalid response: {logged.Message}", failed.ErrorMessage);
        Assert.NotNull(logged.StackTrace);
        if (isJson)
        {
            Assert.IsType<JsonException>(logged, false);
            Assert.IsType<InvalidOperationException>(logged.InnerException);
            Assert.Contains("System.Text.Json", logged.StackTrace, StringComparison.Ordinal);
        }
        else
        {
            Assert.IsType<InvalidOperationException>(logged);
            Assert.IsType<ArgumentException>(logged.InnerException);
            string decodingFrame = status == HttpStatusCode.OK
                ? "JsonHelpers.GetEncoding"
                : "HttpContent.ReadBufferAsString";
            Assert.Contains(decodingFrame, logged.StackTrace, StringComparison.Ordinal);
        }

        IReadOnlyList<KeyValuePair<string, object?>> state =
            Assert.IsType<IReadOnlyList<KeyValuePair<string, object?>>>(log.Arguments[2], false);
        Assert.Equal(LogFieldNames, state.Select(pair => pair.Key));
        Assert.Equal(failed.CommandId, state[0].Value);
        Assert.Equal(nameof(CommandEffectAction), state[1].Value);
        Assert.Equal((int)status, state[2].Value);
        Assert.Equal(format, state[3].Value);
        Assert.DoesNotContain(action.EntityId, state.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    ///     Mapping and transport happen after the executing action, with correlated completion and injected timestamps.
    /// </summary>
    /// <returns>The asynchronous test operation.</returns>
    [Fact]
    public async Task HandleAsyncPostsMappedRequestAfterExecutingAndEmitsSuccessAsync()
    {
        CommandEffectAction action = new("entity-1");
        FakeTimeProvider clock = new(StartedAt);
        bool isSent = false;
        using CommandEffectHttpHandler handler = new(async (request, token) =>
        {
            isSent = true;
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(new("https://commands.test/api/aggregates/test/entity-1/submit"), request.RequestUri);
            Assert.NotNull(request.Content);
            Assert.Equal("application/json", request.Content.Headers.ContentType?.MediaType);
            using JsonDocument body = JsonDocument.Parse(await request.Content.ReadAsStringAsync(token));
            Assert.Equal("mapped", body.RootElement.GetProperty("value").GetString());
            clock.Advance(TimeSpan.FromSeconds(2));
            return CreateResponse(HttpStatusCode.OK, "{\"success\":true}");
        });
        using HttpClient http = CreateClient(handler);
        Mock<IMapper<CommandEffectAction, Dictionary<string, string>>> mapper = CreateMapper(action);
        CommandEffect effect = new(http, mapper.Object, clock);
        await using IAsyncEnumerator<IAction> enumerator = effect
            .HandleAsync(action, null!, TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        Assert.True(await enumerator.MoveNextAsync());
        CommandEffectExecutingAction executing = Assert.IsType<CommandEffectExecutingAction>(enumerator.Current);
        Assert.False(isSent);
        mapper.VerifyNoOtherCalls();
        Assert.Equal(nameof(CommandEffectAction), executing.CommandType);
        Assert.Equal(StartedAt, executing.Timestamp);
        Assert.True(Guid.TryParseExact(executing.CommandId, "N", out Guid _));
        Assert.True(await enumerator.MoveNextAsync());
        CommandEffectSucceededAction succeeded = Assert.IsType<CommandEffectSucceededAction>(enumerator.Current);
        Assert.True(isSent);
        Assert.Equal(executing.CommandId, succeeded.CommandId);
        Assert.Equal(StartedAt.AddSeconds(2), succeeded.Timestamp);
        Assert.False(await enumerator.MoveNextAsync());
        mapper.Verify(value => value.Map(action), Times.Once);
        mapper.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     The original abstraction constructor preserves response failures with absent or failing optional diagnostics.
    /// </summary>
    /// <param name="throwFromHook">Whether a derived effect supplies a failing diagnostic hook.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HandleAsyncPreservesLegacyResponseFailureWithOptionalDiagnosticsAsync(
        bool throwFromHook
    )
    {
        CommandUrlAction action = new("entity-1");
        FakeTimeProvider clock = new(StartedAt);
        Mock<IMapper<CommandUrlAction, CommandUrlRequest>> mapper = new(MockBehavior.Strict);
        mapper.Setup(value => value.Map(action)).Returns(new CommandUrlRequest(1));
        using CommandEffectHttpHandler handler = new((_, _) =>
        {
            clock.Advance(TimeSpan.FromSeconds(2));
            return Task.FromResult(CreateResponse(HttpStatusCode.OK, "{\"success\":\"invalid\"}"));
        });
        using HttpClient http = CreateClient(handler);
        CommandUrlEffect effect = throwFromHook
            ? new ThrowingResponseDiagnosticEffect(http, mapper.Object, clock)
            : new CommandUrlEffect(http, mapper.Object, clock);
        List<IAction> actions = [];
        await foreach (IAction emitted in effect.HandleAsync(action, new(), TestContext.Current.CancellationToken))
        {
            actions.Add(emitted);
        }

        Assert.Equal(2, actions.Count);
        CommandUrlExecutingAction executing = Assert.IsType<CommandUrlExecutingAction>(actions[0]);
        CommandUrlFailedAction failed = Assert.IsType<CommandUrlFailedAction>(actions[1]);
        Assert.Equal(executing.CommandId, failed.CommandId);
        Assert.Equal(StartedAt, executing.Timestamp);
        Assert.Equal(StartedAt.AddSeconds(2), failed.Timestamp);
        Assert.Equal("HttpError", failed.ErrorCode);
        Assert.StartsWith("Invalid response: ", failed.ErrorMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("Diagnostic hook failed.", failed.ErrorMessage, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Mapping failures remain local exceptions rather than being classified as invalid server responses.
    /// </summary>
    /// <returns>The asynchronous test operation.</returns>
    [Fact]
    public async Task HandleAsyncPreservesRequestJsonExceptionsAsync()
    {
        CommandEffectAction action = new("entity-1");
        JsonException expected = new("Request mapping failed.");
        Mock<IMapper<CommandEffectAction, Dictionary<string, string>>> mapper = new(MockBehavior.Strict);
        mapper.Setup(value => value.Map(action)).Throws(expected);
        using CommandEffectHttpHandler handler = new((_, _) =>
            throw new InvalidOperationException("Unexpected HTTP request."));
        using HttpClient http = CreateClient(handler);
        CommandEffect effect = new(http, mapper.Object, new FakeTimeProvider(StartedAt));
        await using IAsyncEnumerator<IAction> enumerator = effect
            .HandleAsync(action, new(), TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        Assert.True(await enumerator.MoveNextAsync());
        Assert.IsType<CommandEffectExecutingAction>(enumerator.Current);
        JsonException actual = await Assert.ThrowsAsync<JsonException>(() => enumerator.MoveNextAsync().AsTask());
        Assert.Same(expected, actual);
        mapper.Verify(value => value.Map(action), Times.Once);
        mapper.VerifyNoOtherCalls();
    }

    /// <summary>
    ///     Critical diagnostic failures propagate from the effect instead of being converted to command outcomes.
    /// </summary>
    /// <param name="exceptionType">The critical exception type.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Theory]
    [InlineData(typeof(OutOfMemoryException))]
    [InlineData(typeof(StackOverflowException))]
    [InlineData(typeof(AccessViolationException))]
    [InlineData(typeof(ThreadInterruptedException))]
    public async Task HandleAsyncPropagatesCriticalDiagnosticFailuresAsync(
        Type exceptionType
    )
    {
        CommandEffectAction action = new("entity-1");
        Exception criticalFailure = Assert.IsType<Exception>(
            Activator.CreateInstance(exceptionType, "Critical diagnostic failure."),
            false);
        Mock<ILogger<CommandEffectAction>> logger = new();
        logger.Setup(value => value.IsEnabled(LogLevel.Warning)).Throws(criticalFailure);
        using CommandEffectHttpHandler handler = new((_, _) =>
            Task.FromResult(CreateResponse(HttpStatusCode.OK, "{\"success\":\"invalid\"}")));
        using HttpClient http = CreateClient(handler);
        CommandEffect effect = new(http, CreateMapper(action).Object, new FakeTimeProvider(StartedAt), logger.Object);
        await using IAsyncEnumerator<IAction> enumerator = effect
            .HandleAsync(action, new(), TestContext.Current.CancellationToken)
            .GetAsyncEnumerator(TestContext.Current.CancellationToken);
        Assert.True(await enumerator.MoveNextAsync());
        Assert.IsType<CommandEffectExecutingAction>(enumerator.Current);
        Exception? actual = await Record.ExceptionAsync(() => enumerator.MoveNextAsync().AsTask());
        Assert.Same(criticalFailure, actual);
    }

    /// <summary>
    ///     Throwing diagnostic providers cannot prevent the original correlated terminal failure.
    /// </summary>
    /// <param name="isJson">Whether to simulate JSON decoding instead of charset decoding.</param>
    /// <param name="status">The HTTP response status.</param>
    /// <param name="throwFromEnabledCheck">Whether the provider fails in IsEnabled instead of Log.</param>
    /// <returns>The asynchronous test operation.</returns>
    [Theory]
    [InlineData(true, HttpStatusCode.OK, true)]
    [InlineData(true, HttpStatusCode.OK, false)]
    [InlineData(false, HttpStatusCode.OK, true)]
    [InlineData(false, HttpStatusCode.OK, false)]
    [InlineData(false, HttpStatusCode.BadRequest, true)]
    [InlineData(false, HttpStatusCode.BadRequest, false)]
    public async Task HandleAsyncRetainsResponseFailureWhenLoggerThrowsAsync(
        bool isJson,
        HttpStatusCode status,
        bool throwFromEnabledCheck
    )
    {
        const string diagnosticMessage = "Diagnostic provider failed.";
        CommandEffectAction action = new("entity-1");
        FakeTimeProvider clock = new(StartedAt);
        Mock<ILogger<CommandEffectAction>> logger = new();
        InvalidOperationException diagnosticFailure = new(diagnosticMessage);
        if (throwFromEnabledCheck)
        {
            logger.Setup(value => value.IsEnabled(LogLevel.Warning)).Throws(diagnosticFailure);
        }
        else
        {
            logger.Setup(value => value.IsEnabled(LogLevel.Warning)).Returns(true);
            logger.Setup(value => value.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
                .Throws(diagnosticFailure);
        }

        using CommandEffectHttpHandler handler = new((_, _) =>
        {
            clock.Advance(TimeSpan.FromSeconds(2));
            HttpResponseMessage response = CreateResponse(status, "{\"success\":\"invalid\"}");
            if (!isJson)
            {
                response.Content.Headers.ContentType!.CharSet = "invalid-charset";
            }

            return Task.FromResult(response);
        });
        using HttpClient http = CreateClient(handler);
        CommandEffect effect = new(http, CreateMapper(action).Object, clock, logger.Object);
        List<IAction> actions = await CollectAsync(effect, action, TestContext.Current.CancellationToken);
        CommandEffectFailedAction failed = AssertFailure(actions, "HttpError");
        Assert.StartsWith("Invalid response: ", failed.ErrorMessage, StringComparison.Ordinal);
        Assert.DoesNotContain(diagnosticMessage, failed.ErrorMessage, StringComparison.Ordinal);
        if (throwFromEnabledCheck)
        {
            Assert.DoesNotContain(logger.Invocations, invocation => invocation.Method.Name == nameof(ILogger.Log));
        }
        else
        {
            Assert.Single(logger.Invocations, invocation => invocation.Method.Name == nameof(ILogger.Log));
        }
    }
}