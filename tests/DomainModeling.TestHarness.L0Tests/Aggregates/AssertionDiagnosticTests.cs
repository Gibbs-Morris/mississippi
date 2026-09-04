using System;
using System.Collections.Generic;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.TestHarness.Aggregates;

using Moq;

using Xunit.Sdk;


namespace Mississippi.DomainModeling.TestHarness.L0Tests.Aggregates;

/// <summary>
///     Verifies failed assertions explain the caller's mistake instead of an incidental downstream failure.
/// </summary>
public sealed class AssertionDiagnosticTests
{
    /// <summary>
    ///     A matching message must not allow an incorrect error code to pass a combined assertion.
    /// </summary>
    [Fact]
    public void CombinedAssertionsShouldRejectIncorrectCodes()
    {
        AggregateScenario<List<string>> scenario = CommandHandlerTestExtensions.ForAggregate<List<string>>()
            .WithHandler<TextCommandHandler>()
            .CreateScenario()
            .When("reject");
        Assert.Throws<XunitException>(() => scenario.ThenFails("wrong-code", "Cannot accept"));
    }

    /// <summary>
    ///     Null assertion arguments should retain their public parameter names in exceptions.
    /// </summary>
    [Fact]
    public void FailureHelpersShouldIdentifyNullAssertionArguments()
    {
        ICommandHandler<string, List<string>> handler = new TextCommandHandler();
        Assert.Throws<ArgumentNullException>(
            "expectedMessage",
            () => handler.ShouldFailWithMessage(null, "reject", null!));
        Assert.Throws<ArgumentNullException>(
            "expectedMessage",
            () => handler.ShouldFailWithMessage(null, "reject", "rejected", null!));
        Assert.Throws<ArgumentNullException>(
            "expectedErrorCode",
            () => handler.ShouldFailWithMessage(null, "reject", null!, "Cannot accept"));
    }

    /// <summary>
    ///     Assertions before execution should identify the missing When step.
    /// </summary>
    /// <param name="assertionKind">The scenario assertion to exercise.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ScenarioAssertionsShouldIdentifyMissingExecution(
        int assertionKind
    )
    {
        AggregateScenario<List<string>> scenario =
            CommandHandlerTestExtensions.ForAggregate<List<string>>().CreateScenario();
        Action assertion = assertionKind switch
        {
            0 => () => scenario.ThenEmits<string>(),
            1 => () => scenario.ThenFails("rejected"),
            var _ => () => scenario.ThenSucceeds(),
        };
        XunitException exception = Assert.Throws<XunitException>(assertion);
        Assert.Contains("When() must be called before", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     An uninitialized result returned by a handler should identify its missing error code.
    /// </summary>
    /// <param name="includeMessage">Whether to assert both error code and message.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScenarioAssertionsShouldIdentifyUninitializedHandlerResults(
        bool includeMessage
    )
    {
        Mock<ICommandHandler<string, List<string>>> handler = new();
        handler.Setup(value => value.Handle("command", It.IsAny<List<string>>()))
            .Returns(default(OperationResult<IReadOnlyList<object>>));
        AggregateScenario<List<string>> scenario = CommandHandlerTestExtensions.ForAggregate<List<string>>()
            .WithHandler(handler.Object)
            .CreateScenario()
            .When("command");
        Action assertion = includeMessage
            ? () => scenario.ThenFails("rejected", "reason")
            : () => scenario.ThenFails("rejected");
        XunitException exception = Assert.Throws<XunitException>(assertion);
        Assert.Contains("Expected an error code from the failed command", exception.Message, StringComparison.Ordinal);
    }
}