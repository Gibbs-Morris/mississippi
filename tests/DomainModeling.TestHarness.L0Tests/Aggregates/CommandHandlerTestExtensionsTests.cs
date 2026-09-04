using System;
using System.Collections.Generic;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.TestHarness.Aggregates;

using Moq;

using Xunit.Sdk;


namespace Mississippi.DomainModeling.TestHarness.L0Tests.Aggregates;

/// <summary>
///     Verifies the assertions offered to command-handler test authors.
/// </summary>
public sealed class CommandHandlerTestExtensionsTests
{
    /// <summary>
    ///     Assertion arguments should be validated before a command executes.
    /// </summary>
    [Fact]
    public void AssertionHelpersShouldRejectNullInputs()
    {
        ICommandHandler<string, List<string>> handler = new TextCommandHandler();
        Assert.Throws<ArgumentNullException>(() =>
            CommandHandlerTestExtensions.Handle<string, List<string>>(null!, null, "command"));
        Assert.Throws<ArgumentNullException>(() => CommandHandlerTestExtensions.Handle(handler, null, null!));
        Assert.Throws<ArgumentNullException>(() =>
            handler.ShouldEmit<string, string, List<string>>(null, "command", null!));
        Assert.Throws<ArgumentNullException>(() => handler.ShouldEmitEvents(null, "command", null!));
        Assert.Throws<ArgumentNullException>(() => handler.ShouldFail(null, "reject", null!));
        Assert.Throws<ArgumentNullException>(() => handler.ShouldFailWithMessage(null, "reject", null!));
        Assert.Throws<ArgumentNullException>(() => handler.ShouldFailWithMessage(null, "reject", "rejected", null!));
    }

    /// <summary>
    ///     Empty successes and incorrect failure messages should be rejected explicitly.
    /// </summary>
    [Fact]
    public void AssertionsShouldRejectEmptySuccessAndIncorrectFailureMessage()
    {
        Mock<ICommandHandler<string, List<string>>> emptyHandler = new();
        emptyHandler.Setup(value => value.Handle("command", It.IsAny<List<string>>()))
            .Returns(OperationResult.Ok<IReadOnlyList<object>>([]));
        ICommandHandler<string, List<string>> rejectedHandler = new TextCommandHandler();
        XunitException empty = Assert.Throws<XunitException>(() => emptyHandler.Object.ShouldSucceed(null, "command"));
        Assert.Contains("at least one event", empty.Message, StringComparison.Ordinal);
        Assert.Throws<XunitException>(() =>
            rejectedHandler.ShouldFailWithMessage(null, "reject", "rejected", "wrong-message"));
    }

    /// <summary>
    ///     Event-list assertions should detect reordered events.
    /// </summary>
    [Fact]
    public void EventAssertionsShouldPreserveOrder()
    {
        Mock<ICommandHandler<string, List<string>>> handler = new();
        handler.Setup(value => value.Handle("command", It.IsAny<List<string>>()))
            .Returns(OperationResult.Ok<IReadOnlyList<object>>(["first", "second"]));
        handler.Object.ShouldEmitEvents(null, "command", "first", "second");
        Assert.Throws<XunitException>(() => handler.Object.ShouldEmitEvents(null, "command", "second", "first"));
    }

    /// <summary>
    ///     Failure assertions should identify an unexpected success and report its event count.
    /// </summary>
    /// <param name="assertionKind">The failure assertion to exercise.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void FailureAssertionsShouldExplainUnexpectedSuccess(
        int assertionKind
    )
    {
        ICommandHandler<string, List<string>> handler = new TextCommandHandler();
        Action assertion = assertionKind switch
        {
            0 => () => handler.ShouldFail(null, "accept"),
            1 => () => handler.ShouldFail(null, "accept", "rejected"),
            2 => () => handler.ShouldFailWithMessage(null, "accept", "Cannot accept"),
            var _ => () => handler.ShouldFailWithMessage(null, "accept", "rejected", "Cannot accept"),
        };
        XunitException exception = Assert.Throws<XunitException>(assertion);
        Assert.Contains("Handler should fail but succeeded with 1 events", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Failure assertions should validate status, error code, and message separately.
    /// </summary>
    [Fact]
    public void FailureHelpersShouldValidateErrorDetails()
    {
        ICommandHandler<string, List<string>> handler = new TextCommandHandler();
        handler.ShouldFail(null, "reject");
        handler.ShouldFail(null, "reject", "rejected");
        handler.ShouldFailWithMessage(null, "reject", "Cannot accept");
        handler.ShouldFailWithMessage(null, "reject", "rejected", "Cannot accept");
        Assert.Empty(handler.HandleEvents(null, "reject"));
        Assert.Throws<XunitException>(() => handler.ShouldFail(null, "accept"));
        Assert.Throws<XunitException>(() => handler.ShouldFail(null, "reject", "wrong"));
        Assert.Throws<XunitException>(() => handler.ShouldFailWithMessage(null, "reject", "wrong"));
        Assert.Throws<XunitException>(() => handler.ShouldFailWithMessage(null, "reject", "wrong", "Cannot accept"));
        Assert.Throws<XunitException>(() => handler.ShouldSucceed(null, "reject"));
    }

    /// <summary>
    ///     Null commands should be rejected before invoking a user handler that accepts them.
    /// </summary>
    [Fact]
    public void HandleShouldRejectNullCommandsBeforeInvokingHandler()
    {
        Mock<ICommandHandler<string, List<string>>> handler = new();
        handler.Setup(value => value.Handle(It.IsAny<string>(), It.IsAny<List<string>>()))
            .Returns(OperationResult.Ok<IReadOnlyList<object>>([]));
        ArgumentNullException exception =
            Assert.Throws<ArgumentNullException>(() =>
                CommandHandlerTestExtensions.Handle(handler.Object, null, null!));
        Assert.Equal("command", exception.ParamName);
        handler.Verify(value => value.Handle(It.IsAny<string>(), It.IsAny<List<string>>()), Times.Never);
    }

    /// <summary>
    ///     Every success assertion should identify a rejected command before checking its event payload.
    /// </summary>
    /// <param name="assertionKind">The success assertion to exercise.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void SuccessAssertionsShouldExplainCommandRejection(
        int assertionKind
    )
    {
        ICommandHandler<string, List<string>> handler = new TextCommandHandler();
        Action assertion = assertionKind switch
        {
            0 => () => handler.ShouldEmit(null, "reject", "event"),
            1 => () => handler.ShouldEmitEvents(null, "reject", "event"),
            var _ => () => handler.ShouldSucceed(null, "reject"),
        };
        XunitException exception = Assert.Throws<XunitException>(assertion);
        Assert.Contains(
            "Handler should succeed, but failed with: rejected - Cannot accept this command.",
            exception.Message,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Success helpers should pass state to handlers and expose the emitted events.
    /// </summary>
    [Fact]
    public void SuccessHelpersShouldUseSuppliedOrDefaultState()
    {
        ICommandHandler<string, List<string>> handler = new TextCommandHandler();
        OperationResult<IReadOnlyList<object>> result = handler.Handle(null, "first");
        IReadOnlyList<object> events = handler.HandleEvents(["prior"], "second");
        Assert.True(result.Success);
        Assert.Equal(["0:first"], result.Value);
        Assert.Equal(["1:second"], events);
        handler.ShouldEmit(null, "first", "0:first");
        handler.ShouldEmitEvents(["prior"], "second", "1:second");
        Assert.Equal(["0:first"], handler.ShouldSucceed(null, "first"));
        Assert.Throws<XunitException>(() => handler.ShouldEmit(null, "first", "wrong"));
    }
}