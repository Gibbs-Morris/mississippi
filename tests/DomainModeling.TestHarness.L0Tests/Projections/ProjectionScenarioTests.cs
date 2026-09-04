using System;
using System.Collections.Generic;

using Mississippi.DomainModeling.TestHarness.Projections;

using Xunit.Sdk;


namespace Mississippi.DomainModeling.TestHarness.L0Tests.Projections;

/// <summary>
///     Verifies projection replay and fluent expectation behavior.
/// </summary>
public sealed class ProjectionScenarioTests
{
    /// <summary>
    ///     Direct reducer execution should use the configured initial state for every independent run.
    /// </summary>
    [Fact]
    public void DirectReplayShouldStartFromConfiguredState()
    {
        ReducerTestHarness<List<string>> harness = ReducerTestExtensions.ForProjection<List<string>>()
            .WithReducer(new AppendTextReducer());
        Assert.Equal(["one"], harness.ApplyEvent("one"));
        Assert.Equal(["two", "three"], harness.ApplyEvents("two", "three"));
        Assert.Empty(harness.ApplyEvents());
        harness.WithInitialState(["seed"]);
        Assert.Equal(["seed", "four"], harness.ApplyEvent("four"));
        Assert.Equal(["seed", "five", "six"], harness.ApplyEvents("five", "six"));
    }

    /// <summary>
    ///     Historical and current events should be replayed in order without mutating the initial state.
    /// </summary>
    [Fact]
    public void ProjectionScenariosShouldReplayEventsAndPreserveInitialState()
    {
        List<string> initial = ["initial"];
        ReducerTestHarness<List<string>> harness = ReducerTestExtensions.ForProjection<List<string>>()
            .WithInitialState(initial)
            .WithReducer<AppendTextReducer>();
        ProjectionScenario<List<string>> scenario = harness.CreateScenario();
        ProjectionScenario<List<string>> result = scenario.Given("earlier", "recent")
            .When("current")
            .ThenEquals(["initial", "earlier", "recent", "current"])
            .ThenShouldBe(["initial", "earlier", "recent", "current"])
            .ThenAssert(state => Assert.Equal(4, state.Count))
            .ThenShouldSatisfy(state => Assert.Equal("current", state[3]))
            .ThenShouldSatisfy(state => state.Count == 4, "every event must be applied");
        Assert.Same(scenario, result);
        Assert.Equal(["earlier", "recent", "current"], scenario.AppliedEvents);
        Assert.Equal(["initial"], initial);
        Assert.Equal(["initial"], harness.CreateScenario().State);
        Assert.Throws<XunitException>(() => scenario.ThenEquals(["incorrect"]));
        Assert.Throws<XunitException>(() => scenario.ThenShouldSatisfy(
            state => state.Count == 0,
            "state must be empty"));
    }

    /// <summary>
    ///     Both direct and scenario replay should reject unknown event types.
    /// </summary>
    [Fact]
    public void ReplayShouldRejectEventsWithoutReducers()
    {
        ReducerTestHarness<List<string>> harness = ReducerTestExtensions.ForProjection<List<string>>()
            .WithReducer<AppendTextReducer>();
        object unknownEvent = new();
        Assert.Throws<InvalidOperationException>(() => harness.ApplyEvent(unknownEvent));
        Assert.Throws<InvalidOperationException>(() => harness.ApplyEvents("known", unknownEvent));
        Assert.Throws<InvalidOperationException>(() => harness.CreateScenario().Given(unknownEvent));
        Assert.Throws<InvalidOperationException>(() => harness.CreateScenario().When(unknownEvent));
    }

    /// <summary>
    ///     Projection entry points should validate null configuration, events, and expectations.
    /// </summary>
    [Fact]
    public void ReplayShouldRejectNullInputs()
    {
        ReducerTestHarness<List<string>> harness = ReducerTestExtensions.ForProjection<List<string>>();
        ProjectionScenario<List<string>> scenario = harness.CreateScenario();
        object[] missingEvents = null!;
        Assert.Throws<ArgumentNullException>(() => harness.WithInitialState(null!));
        Assert.Throws<ArgumentNullException>(() => harness.WithReducer(null!));
        Assert.Throws<ArgumentNullException>(() => harness.ApplyEvent<string>(null!));
        Assert.Throws<ArgumentNullException>(() => harness.ApplyEvents(null!));
        Assert.Throws<ArgumentNullException>(() => harness.ApplyEvents([null!]));
        Assert.Throws<ArgumentNullException>(() => scenario.Given((object)null!));
        Assert.Throws<ArgumentNullException>(() => scenario.Given(missingEvents));
        Assert.Throws<ArgumentNullException>(() => scenario.When(null!));
        Assert.Throws<ArgumentNullException>(() => scenario.ThenEquals(null!));
        Assert.Throws<ArgumentNullException>(() => scenario.ThenAssert(null!));
        Assert.Throws<ArgumentNullException>(() => scenario.ThenShouldSatisfy(null!, "reason"));
    }

    /// <summary>
    ///     Replay should ignore generic interfaces unrelated to event reducers.
    /// </summary>
    [Fact]
    public void ReplayShouldSelectOnlyReducerInterfaces()
    {
        ReducerTestHarness<List<string>> harness = ReducerTestExtensions.ForProjection<List<string>>()
            .WithReducer<MultiInterfaceTextReducer>();
        Assert.Equal(["first", "second"], harness.ApplyEvents("first", "second"));
        ProjectionScenario<List<string>> scenario = harness.CreateScenario().Given("history").When("next");
        Assert.Equal(["history", "next"], scenario.State);
        Assert.ThrowsAny<XunitException>(() => scenario.ThenAssert(Assert.Empty));
        Assert.ThrowsAny<XunitException>(() => scenario.ThenShouldSatisfy(Assert.Empty));
        Uri unrelated = new("https://example.com");
        InvalidOperationException missing =
            Assert.Throws<InvalidOperationException>(() => harness.ApplyEvent(unrelated));
        InvalidOperationException missingBatch =
            Assert.Throws<InvalidOperationException>(() => harness.ApplyEvents("known", unrelated));
        InvalidOperationException missingScenario =
            Assert.Throws<InvalidOperationException>(() => scenario.When(unrelated));
        Assert.Contains("No reducer registered for event type Uri", missing.Message, StringComparison.Ordinal);
        Assert.Contains("No reducer registered for event type Uri", missingBatch.Message, StringComparison.Ordinal);
        Assert.Contains("No reducer registered for event type Uri", missingScenario.Message, StringComparison.Ordinal);
    }
}