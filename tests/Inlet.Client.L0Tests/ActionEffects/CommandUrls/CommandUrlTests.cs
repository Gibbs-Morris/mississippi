using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Extensions.Time.Testing;

using Mississippi.Common.Abstractions.Mapping;
using Mississippi.Reservoir.Abstractions.Actions;

using Moq;


namespace Mississippi.Inlet.Client.L0Tests.ActionEffects.CommandUrls;

/// <summary>Verifies entity identity and lifecycle across command URL construction.</summary>
public sealed class CommandUrlTests
{
    /// <summary>Allows an override to own identifiers outside the default route contract.</summary>
    /// <param name="entityId">The identifier handled by the application endpoint.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData("customer/42")]
    [InlineData(".")]
    [InlineData("..")]
    public async Task HandleAsyncPreservesEndpointOverride(
        string entityId
    )
    {
        using CommandUrlHandler handler = new();
        using HttpClient http = new(handler)
        {
            BaseAddress = new("https://example.test"),
        };
        CommandUrlAction action = new(entityId);
        Mock<IMapper<CommandUrlAction, CommandUrlRequest>> mapper = new();
        mapper.Setup(value => value.Map(action)).Returns(new CommandUrlRequest(42));
        OverriddenCommandUrlEffect effect = new(http, mapper.Object, new FakeTimeProvider(DateTimeOffset.UnixEpoch));
        List<IAction> actions = [];
        await foreach (IAction result in effect.HandleAsync(action, new(), TestContext.Current.CancellationToken))
        {
            actions.Add(result);
        }

        Assert.Equal(2, actions.Count);
        CommandUrlExecutingAction executing = Assert.IsType<CommandUrlExecutingAction>(actions[0]);
        CommandUrlSucceededAction succeeded = Assert.IsType<CommandUrlSucceededAction>(actions[1]);
        Assert.Equal(executing.CommandId, succeeded.CommandId);
        Uri uri = Assert.IsType<Uri>(handler.RequestUri);
        Assert.Equal("/custom/submit", uri.AbsolutePath);
        Assert.Equal(1, handler.CallCount);
        mapper.Verify(value => value.Map(action), Times.Once);
    }

    /// <summary>Preserves a supported identifier as exactly one escaped segment.</summary>
    /// <param name="entityId">The original identifier.</param>
    /// <param name="escapedSegment">The expected wire segment, independently specified.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData("customer-42", "customer-42")]
    [InlineData("customer#42", "customer%2342")]
    [InlineData("customer?region=42", "customer%3Fregion%3D42")]
    [InlineData("customer%2F42", "customer%252F42")]
    [InlineData("customer%2f42", "customer%252f42")]
    [InlineData("customer%2342", "customer%252342")]
    [InlineData("customer%3F42", "customer%253F42")]
    [InlineData("customer 42", "customer%2042")]
    [InlineData("customer+42", "customer%2B42")]
    [InlineData("客户42", "%E5%AE%A2%E6%88%B742")]
    [InlineData("customer\\42", "customer%5C42")]
    [InlineData("%2E", "%252E")]
    [InlineData("%2e%2e", "%252e%252e")]
    [InlineData("customer..42", "customer..42")]
    public async Task HandleAsyncPreservesSupportedEntitySegment(
        string entityId,
        string escapedSegment
    )
    {
        using CommandUrlHandler handler = new();
        using HttpClient http = new(handler)
        {
            BaseAddress = new("https://example.test"),
        };
        CommandUrlAction action = new(entityId);
        Mock<IMapper<CommandUrlAction, CommandUrlRequest>> mapper = new();
        mapper.Setup(value => value.Map(action)).Returns(new CommandUrlRequest(42));
        FakeTimeProvider time = new(DateTimeOffset.UnixEpoch);
        CommandUrlEffect effect = new(http, mapper.Object, time);
        List<IAction> actions = [];
        await foreach (IAction result in effect.HandleAsync(action, new(), TestContext.Current.CancellationToken))
        {
            actions.Add(result);
        }

        Assert.Equal(2, actions.Count);
        CommandUrlExecutingAction executing = Assert.IsType<CommandUrlExecutingAction>(actions[0]);
        CommandUrlSucceededAction succeeded = Assert.IsType<CommandUrlSucceededAction>(actions[1]);
        Assert.Equal(executing.CommandId, succeeded.CommandId);
        Assert.Equal(DateTimeOffset.UnixEpoch, succeeded.Timestamp);
        Assert.Equal(1, handler.CallCount);
        Uri uri = Assert.IsType<Uri>(handler.RequestUri);
        Assert.Equal($"/api/aggregates/customer/{escapedSegment}/submit", uri.AbsolutePath);
        Assert.Empty(uri.Query);
        Assert.Empty(uri.Fragment);
        mapper.Verify(value => value.Map(action), Times.Once);
    }

    /// <summary>Rejects ambiguous default segments before mapping or transport.</summary>
    /// <param name="entityId">The identifier the default route cannot preserve.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData("customer/42")]
    [InlineData(".")]
    [InlineData("..")]
    public async Task HandleAsyncRejectsAmbiguousDefaultSegment(
        string entityId
    )
    {
        using CommandUrlHandler handler = new();
        using HttpClient http = new(handler)
        {
            BaseAddress = new("https://example.test"),
        };
        CommandUrlAction action = new(entityId);
        Mock<IMapper<CommandUrlAction, CommandUrlRequest>> mapper = new();
        mapper.Setup(value => value.Map(action)).Returns(new CommandUrlRequest(42));
        CommandUrlEffect effect = new(http, mapper.Object, new FakeTimeProvider(DateTimeOffset.UnixEpoch));
        List<IAction> actions = [];
        await foreach (IAction result in effect.HandleAsync(action, new(), TestContext.Current.CancellationToken))
        {
            actions.Add(result);
        }

        Assert.Equal(2, actions.Count);
        CommandUrlExecutingAction executing = Assert.IsType<CommandUrlExecutingAction>(actions[0]);
        CommandUrlFailedAction failed = Assert.IsType<CommandUrlFailedAction>(actions[1]);
        Assert.Equal(executing.CommandId, failed.CommandId);
        Assert.Equal("HttpError", failed.ErrorCode);
        Assert.Contains("entity ID", failed.ErrorMessage, StringComparison.Ordinal);
        Assert.Equal(0, handler.CallCount);
        Assert.Null(handler.RequestUri);
        mapper.Verify(value => value.Map(It.IsAny<CommandUrlAction>()), Times.Never);
    }
}