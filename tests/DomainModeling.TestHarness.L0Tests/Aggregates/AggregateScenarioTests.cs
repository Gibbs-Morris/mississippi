using System;
using System.Collections.Generic;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.TestHarness.Aggregates;
using Mississippi.Tributary.Abstractions;

using Moq;

using Xunit.Sdk;


namespace Mississippi.DomainModeling.TestHarness.L0Tests.Aggregates;

/// <summary>
///     Verifies aggregate test scenarios preserve replay, execution, and assertion contracts.
/// </summary>
public sealed class AggregateScenarioTests
{
    private static AggregateTestHarness<List<string>> CreateHarness() =>
        CommandHandlerTestExtensions.ForAggregate<List<string>>()
            .WithHandler<TextCommandHandler>()
            .WithReducer<AppendTextReducer>();

    /// <summary>
    ///     Failed commands retain their error details and do not emit or apply events.
    /// </summary>
    [Fact]
    public void FailedCommandsShouldRetainStateAndErrorDetails()
    {
        AggregateScenario<List<string>> scenario = CreateHarness().CreateScenario().Given("history");
        scenario.When("reject").ThenFails("rejected").ThenFails("rejected", "Cannot accept");
        Assert.Empty(scenario.EmittedEvents);
        Assert.Equal(["history"], scenario.State);
        Assert.Equal(["history"], scenario.AllAppliedEvents);
        Assert.Throws<XunitException>(() => scenario.ThenFails("wrong-code"));
        Assert.Throws<XunitException>(() => scenario.ThenFails("rejected", "wrong-message"));
        Assert.Throws<XunitException>(() => scenario.ThenSucceeds());
    }

    /// <summary>
    ///     Null configuration and scenario arguments should be rejected at their public entry point.
    /// </summary>
    [Fact]
    public void HarnessAndScenarioShouldRejectNullArguments()
    {
        AggregateTestHarness<List<string>> harness = CreateHarness();
        AggregateScenario<List<string>> scenario = harness.CreateScenario();
        object[] missingEvents = null!;
        Assert.Throws<ArgumentNullException>(() => harness.WithHandler(null!));
        Assert.Throws<ArgumentNullException>(() => harness.WithReducer(null!));
        Assert.Throws<ArgumentNullException>(() => harness.WithInitialState(null!));
        Assert.Throws<ArgumentNullException>(() => scenario.Given((object)null!));
        Assert.Throws<ArgumentNullException>(() => scenario.Given(missingEvents));
        Assert.Throws<ArgumentNullException>(() => scenario.When(null!));
        Assert.Throws<ArgumentNullException>(() => scenario.ThenEmitsEvents(null!));
        Assert.Throws<ArgumentNullException>(() => scenario.ThenState(null!));
        Assert.Throws<ArgumentNullException>(() => scenario.ThenFails(null!));
        Assert.Throws<ArgumentNullException>(() => scenario.ThenFails("rejected", null!));
    }

    /// <summary>
    ///     Missing commands and rejected commands should produce distinct diagnostics.
    /// </summary>
    [Fact]
    public void ScenarioAssertionsShouldExplainMissingCommandAndFailureStatus()
    {
        AggregateScenario<List<string>> scenario = CreateHarness().CreateScenario();
        XunitException missing = Assert.Throws<XunitException>(() => scenario.ThenFails("rejected", "Cannot accept"));
        Assert.Contains("When() must be called", missing.Message, StringComparison.Ordinal);
        scenario.When("accepted");
        XunitException status = Assert.Throws<XunitException>(() => scenario.ThenFails("rejected"));
        Assert.Contains(
            "Command should have failed but succeeded with 1 events",
            status.Message,
            StringComparison.Ordinal);
        Assert.Equal(
            "expectedErrorCode",
            Assert.Throws<ArgumentNullException>(() => scenario.ThenFails(null!, "reason")).ParamName);
    }

    /// <summary>
    ///     Assertions should reject a missing command, missing event, or incorrect event count.
    /// </summary>
    [Fact]
    public void ScenarioAssertionsShouldRejectInvalidExpectations()
    {
        AggregateScenario<List<string>> scenario = CreateHarness().CreateScenario();
        Assert.Throws<XunitException>(() => scenario.ThenEmits<string>());
        Assert.Throws<XunitException>(() => scenario.ThenEmitsEvents());
        Assert.Throws<XunitException>(() => scenario.ThenState(_ => { }));
        Assert.Throws<XunitException>(() => scenario.ThenFails("rejected"));
        Assert.Throws<XunitException>(() => scenario.ThenSucceeds());
        scenario.When("accepted");
        Assert.Throws<XunitException>(() => scenario.ThenEmits<Uri>());
        Assert.Throws<XunitException>(() => scenario.ThenEmitsEvents());
        Assert.Throws<XunitException>(() => scenario.ThenFails("rejected"));
        Assert.Same(scenario, scenario.ThenEmits<string>());
    }

    /// <summary>
    ///     User-supplied event and state assertions must execute and propagate their failures.
    /// </summary>
    [Fact]
    public void ScenarioCallbacksShouldPropagateAssertionFailures()
    {
        AggregateScenario<List<string>> scenario = CreateHarness().CreateScenario().When("accepted");
        Assert.ThrowsAny<XunitException>(() => scenario.ThenEmits<string>(value => Assert.Equal("wrong", value)));
        Assert.ThrowsAny<XunitException>(() => scenario.ThenEmitsEvents(value => Assert.Equal("wrong", value)));
        Assert.ThrowsAny<XunitException>(() => scenario.ThenState(Assert.Empty));
        XunitException failure = Assert.Throws<XunitException>(() => scenario.ThenFails("rejected", "Cannot accept"));
        Assert.Contains(
            "Command should have failed but succeeded with 1 events",
            failure.Message,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Unsupported commands and events should fail with a diagnostic naming the missing type.
    /// </summary>
    [Fact]
    public void ScenariosShouldRejectUnregisteredCommandsAndEvents()
    {
        AggregateScenario<List<string>> scenario = CreateHarness().CreateScenario();
        InvalidOperationException commandError = Assert.Throws<InvalidOperationException>(() => scenario.When(new()));
        InvalidOperationException eventError =
            Assert.Throws<InvalidOperationException>(() => scenario.Given(new object()));
        Assert.Contains(
            "No handler registered for command type Object",
            commandError.Message,
            StringComparison.Ordinal);
        Assert.Contains("No reducer registered for event type Object", eventError.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Given events establish state before the handler runs, with emitted events recorded separately.
    /// </summary>
    /// <param name="useInstances">Whether to register explicit handler and reducer instances.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScenariosShouldReplayBeforeExecutingCommands(
        bool useInstances
    )
    {
        AggregateTestHarness<List<string>> harness = useInstances
            ? CommandHandlerTestExtensions.ForAggregate<List<string>>()
                .WithHandler(new TextCommandHandler())
                .WithReducer(new AppendTextReducer())
            : CreateHarness();
        List<string> initial = ["initial"];
        AggregateScenario<List<string>> scenario = harness.WithInitialState(initial).CreateScenario();
        AggregateScenario<List<string>> result = scenario.Given("earlier", "recent")
            .When("next")
            .ThenSucceeds()
            .ThenEmits<string>(value => Assert.Equal("3:next", value))
            .ThenEmitsEvents(value => Assert.Equal("3:next", value))
            .ThenState(state => Assert.Equal(["initial", "earlier", "recent", "3:next"], state));
        Assert.Same(scenario, result);
        Assert.Equal(["3:next"], scenario.EmittedEvents);
        Assert.Equal(["earlier", "recent", "3:next"], scenario.AllAppliedEvents);
        Assert.Equal(["initial"], initial);
        Assert.Equal(["initial"], harness.CreateScenario().State);
    }

    /// <summary>
    ///     Dispatch should ignore unrelated generic interfaces on handlers and reducers.
    /// </summary>
    [Fact]
    public void ScenariosShouldSelectOnlyCommandAndReducerInterfaces()
    {
        AggregateScenario<List<string>> scenario = CommandHandlerTestExtensions.ForAggregate<List<string>>()
            .WithHandler<MultiInterfaceTextHandler>()
            .WithReducer<MultiInterfaceTextReducer>()
            .CreateScenario();
        scenario.Given("history").When("next");
        Assert.Equal(["history", "1:next"], scenario.State);
        Uri unrelated = new("https://example.com");
        InvalidOperationException missingCommand =
            Assert.Throws<InvalidOperationException>(() => scenario.When(unrelated));
        InvalidOperationException missingReducer =
            Assert.Throws<InvalidOperationException>(() => scenario.Given(unrelated));
        Assert.Contains("No handler registered for command type Uri", missingCommand.Message, StringComparison.Ordinal);
        Assert.Contains("No reducer registered for event type Uri", missingReducer.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Success assertions must reject emitted failure events even when the handler returns success.
    /// </summary>
    [Fact]
    public void SuccessAssertionsShouldRejectFailureEvents()
    {
        Mock<ICommandHandler<string, List<string>>> handler = new();
        handler.Setup(value => value.Handle("command", It.IsAny<List<string>>()))
            .Returns(OperationResult.Ok<IReadOnlyList<object>>([new FailedEvent()]));
        Mock<IEventReducer<FailedEvent, List<string>>> reducer = new();
        reducer.Setup(value => value.Reduce(It.IsAny<List<string>>(), It.IsAny<FailedEvent>())).Returns([]);
        AggregateScenario<List<string>> scenario = CommandHandlerTestExtensions.ForAggregate<List<string>>()
            .WithHandler(handler.Object)
            .WithReducer(reducer.Object)
            .CreateScenario()
            .When("command");
        XunitException exception = Assert.Throws<XunitException>(() => scenario.ThenSucceeds());
        Assert.Contains(
            "Expected success events, but got a failure event",
            exception.Message,
            StringComparison.Ordinal);
    }
}