using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Mississippi.DomainModeling.Abstractions;
using Mississippi.DomainModeling.TestHarness.Aggregates;
using Mississippi.DomainModeling.TestHarness.Effects;
using Mississippi.DomainModeling.TestHarness.Projections;
using Mississippi.Tributary.Abstractions;

using Moq;

using Xunit.Sdk;


namespace Mississippi.DomainModeling.TestHarness.L0Tests;

/// <summary>
///     Verifies public assertion contracts independently of sample consumers.
/// </summary>
public sealed class AssertionContractTests
{
    private static ICommandHandler<object, object> CreateHandler(
        params object[] events
    ) =>
        CreateHandler(OperationResult.Ok<IReadOnlyList<object>>(events));

    private static ICommandHandler<object, object> CreateHandler(
        OperationResult<IReadOnlyList<object>> result
    )
    {
        Mock<ICommandHandler<object, object>> handler = new();
        handler.Setup(value => value.Handle(It.IsAny<object>(), It.IsAny<object>())).Returns(result);
        return handler.Object;
    }

    /// <summary>
    ///     Cycles fail as assertions while repeated acyclic references remain valid.
    /// </summary>
    [Fact]
    public void CyclesDoNotCauseUnboundedRecursion()
    {
        object[] expected = new object[1];
        object[] actual = new object[1];
        expected[0] = expected;
        actual[0] = actual;
        Assert.ThrowsAny<XunitException>(() => CreateHandler(actual).ShouldEmitEvents(null, new(), expected));
        object repeated = new
        {
            Value = 1,
        };
        CreateHandler(
                new
                {
                    Value = 1,
                },
                new
                {
                    Value = 1,
                })
            .ShouldEmitEvents(null, new(), repeated, repeated);
    }

    /// <summary>
    ///     Dictionaries match keys while sequence ordering still applies to their values.
    /// </summary>
    [Fact]
    public void DictionaryInsertionOrderDoesNotChangeEquivalence()
    {
        Dictionary<string, int[]> expected = new()
        {
            ["a"] = [1, 2],
            ["b"] = [3],
        };
        Dictionary<string, int[]> actual = new()
        {
            ["b"] = [3],
            ["a"] = [1, 2],
        };
        CreateHandler(actual).ShouldEmitEvents(null, new(), expected);
        actual["a"] = [2, 1];
        Assert.ThrowsAny<XunitException>(() => CreateHandler(actual).ShouldEmitEvents(null, new(), expected));
        actual.Remove("a");
        actual["c"] = [1, 2];
        Assert.ThrowsAny<XunitException>(() => CreateHandler(actual).ShouldEmitEvents(null, new(), expected));
    }

    /// <summary>
    ///     Dispatch assertions return the matching command and honor the entity ID.
    /// </summary>
    [Fact]
    public void DispatchAssertionsSelectTheRequestedTarget()
    {
        object command = new();
        IReadOnlyList<(Type AggregateType, string EntityId, object Command)> commands =
        [
            (typeof(object), "first", "other"),
            (typeof(object), "second", command),
        ];
        Assert.Equal("other", commands.ShouldHaveDispatched<string>());
        Assert.Same(command, commands.ShouldHaveDispatchedTo<object>("second").Command);
        Assert.ThrowsAny<XunitException>(() => commands.ShouldHaveDispatchedTo<object>("missing"));
        Assert.ThrowsAny<XunitException>(() => commands.ShouldHaveNoDispatches());
    }

    /// <summary>
    ///     Structural comparison selects expected members and supports property-to-field matching.
    /// </summary>
    [Fact]
    public void ExpectedMembersAllowAdditionalActualData()
    {
        CreateHandler(
                new
                {
                    Value = 1,
                    Extra = 2,
                })
            .ShouldEmit(
                null,
                new(),
                new
                {
                    Value = 1,
                });
        CreateHandler(
                new
                {
                    Value = 1,
                    Extra = 2,
                })
            .ShouldEmitEvents(
                null,
                new(),
                new
                {
                    Value = 1,
                });
        CreateHandler((1, "value"))
            .ShouldEmitEvents(
                null,
                new(),
                new
                {
                    Item1 = 1,
                    Item2 = "value",
                });
        Assert.ThrowsAny<XunitException>(() => CreateHandler(
                new
                {
                    Different = 1,
                })
            .ShouldEmitEvents(
                null,
                new(),
                new
                {
                    Value = 1,
                }));
    }

    /// <summary>
    ///     A failed handler preserves its error details without reading failed result data.
    /// </summary>
    [Fact]
    public void FailedHandlerStopsSuccessAssertions()
    {
        ICommandHandler<object, object> handler =
            CreateHandler(OperationResult.Fail<IReadOnlyList<object>>("denied", "account closed"));
        Action[] assertions =
        [
            () => handler.ShouldEmit(null, new(), new object()),
            () => handler.ShouldEmitEvents(null, new()),
            () => handler.ShouldSucceed(null, new()),
        ];
        Assert.All(
            assertions,
            assertion =>
            {
                string message = Assert.ThrowsAny<XunitException>(assertion).Message;
                Assert.Contains("denied", message, StringComparison.Ordinal);
                Assert.Contains("account closed", message, StringComparison.Ordinal);
            });
        handler.ShouldFail(null, new(), "denied");
        handler.ShouldFailWithMessage(null, new(), "denied", "closed");
    }

    /// <summary>
    ///     Scenario prerequisites stop callbacks and produce assertion failures.
    /// </summary>
    [Fact]
    public void MissingCommandStopsEveryScenarioAssertion()
    {
        AggregateScenario<object> scenario = new AggregateTestHarness<object>().CreateScenario();
        bool called = false;
        Action[] assertions =
        [
            () => scenario.ThenEmits<object>(_ => called = true),
            () => scenario.ThenEmitsEvents(_ => called = true),
            () => scenario.ThenState(_ => called = true),
            () => scenario.ThenSucceeds(),
            () => scenario.ThenFails("error"),
            () => scenario.ThenFails("error", "message"),
        ];
        Assert.All(
            assertions,
            assertion => Assert.Contains(
                "When() must be called",
                Assert.ThrowsAny<XunitException>(assertion).Message,
                StringComparison.Ordinal));
        Assert.False(called);
    }

    /// <summary>
    ///     Missing dispatches fail before a lookup can produce an incidental exception.
    /// </summary>
    [Fact]
    public void MissingDispatchesProduceAssertionFailures()
    {
        IReadOnlyList<(Type AggregateType, string EntityId, object Command)> commands = [];
        EffectTestResult result = new(commands);
        Assert.ThrowsAny<XunitException>(() => commands.ShouldHaveDispatched<string>());
        Assert.ThrowsAny<XunitException>(() => result.ShouldHaveDispatched<string>());
        Assert.ThrowsAny<XunitException>(() => commands.ShouldHaveDispatchedTo<object>());
        Assert.ThrowsAny<XunitException>(() => result.ShouldHaveDispatchedTo<object>());
        Assert.Same(result, result.ShouldHaveNoDispatches());
    }

    /// <summary>
    ///     Missing events and count mismatches fail before invoking callbacks.
    /// </summary>
    [Fact]
    public void MissingEventsStopCallbacks()
    {
        AggregateScenario<object> scenario = new AggregateTestHarness<object>().WithHandler(CreateHandler())
            .CreateScenario()
            .When(new());
        bool called = false;
        XunitException missing = Assert.ThrowsAny<XunitException>(() => scenario.ThenEmits<string>(_ => called = true));
        Assert.Contains("Expected event of type String", missing.Message, StringComparison.Ordinal);
        Assert.ThrowsAny<XunitException>(() => scenario.ThenEmitsEvents(_ => called = true));
        Assert.False(called);
    }

    /// <summary>
    ///     Empty data can be compared, while missing or unexpected member values fail as assertions.
    /// </summary>
    [Fact]
    public void NullAndEmptyEventDataUseXunitAssertions()
    {
        CreateHandler(new object()).ShouldEmit(null, new(), new object());
        Assert.ThrowsAny<XunitException>(() => CreateHandler(
                new
                {
                    Value = (object?)null,
                })
            .ShouldEmit(
                null,
                new(),
                new
                {
                    Value = new object(),
                }));
        Assert.ThrowsAny<XunitException>(() => CreateHandler(
                new
                {
                    Value = new object(),
                })
            .ShouldEmit(
                null,
                new(),
                new
                {
                    Value = (object?)null,
                }));
    }

    /// <summary>
    ///     Ordered assertions reject both outer and nested sequence reordering.
    /// </summary>
    [Fact]
    public void OrderedEventsPreserveEverySequenceLevel()
    {
        int[] ordered = [1, 2];
        int[] reversed = [2, 1];
        Assert.ThrowsAny<XunitException>(() => CreateHandler(
                new
                {
                    Value = 2,
                },
                new
                {
                    Value = 1,
                })
            .ShouldEmitEvents(
                null,
                new(),
                new
                {
                    Value = 1,
                },
                new
                {
                    Value = 2,
                }));
        Assert.ThrowsAny<XunitException>(() => CreateHandler(
                new
                {
                    Items = reversed,
                })
            .ShouldEmitEvents(
                null,
                new(),
                new
                {
                    Items = ordered,
                }));
        CreateHandler(
                new
                {
                    Items = ordered,
                })
            .ShouldEmitEvents(
                null,
                new(),
                new
                {
                    Items = (int[])ordered.Clone(),
                });
    }

    /// <summary>
    ///     Projection helpers preserve structural matching and unordered collection contents.
    /// </summary>
    [Fact]
    public void ProjectionAssertionsUseExpectedMembers()
    {
        int[] ordered = [1, 2];
        int[] reversed = [2, 1];
        int[] incomplete = [1];
        object expected = new
        {
            Items = ordered,
        };
        Mock<IEventReducer<object, object>> reducer = new();
        reducer.Setup(value => value.Reduce(It.IsAny<object>(), It.IsAny<object>()))
            .Returns(
                new
                {
                    Items = reversed,
                    Extra = true,
                });
        reducer.Object.ShouldProduce(null, new(), expected);
        ProjectionScenario<object> scenario = new ReducerTestHarness<object>()
            .WithReducer(reducer.Object)
            .CreateScenario()
            .When(new());
        Assert.Same(scenario, scenario.ThenEquals(expected));
        Assert.ThrowsAny<XunitException>(() => scenario.ThenEquals(
            new
            {
                Items = incomplete,
            }));
    }

    /// <summary>
    ///     Exception helpers retain assignability, aggregate unwrapping, and wildcard matching.
    /// </summary>
    [Fact]
    public void ReducerExceptionsKeepAssignableAndWildcardSemantics()
    {
        Mock<IEventReducer<object, object>> reducer = new();
        reducer.Setup(value => value.Reduce(It.IsAny<object>(), It.IsAny<object>()))
            .Throws(new AggregateException(new InvalidOperationException("TIMED OUT"), new ArgumentException("other")));
        reducer.Object.ShouldThrow<InvalidOperationException, object, object>(null, new(), "time? *out");
        Assert.ThrowsAny<XunitException>(() =>
            reducer.Object.ShouldThrow<InvalidOperationException, object, object>(null, new(), "unrelated"));
        reducer.Setup(value => value.Reduce(It.IsAny<object>(), It.IsAny<object>()))
            .Throws(new TaskCanceledException("operation canceled"));
        reducer.Object.ShouldThrow<OperationCanceledException, object, object>(null, new());
    }

    /// <summary>
    ///     Unordered comparison preserves cardinality and duplicate occurrences.
    /// </summary>
    [Fact]
    public void UnorderedEventsPreserveCollectionContents()
    {
        int[] ordered = [1, 2];
        int[] reversed = [2, 1];
        int[] extra = [1, 2, 3];
        int[] duplicate = [1, 1];
        byte[] orderedBytes = [1, 2];
        byte[] reversedBytes = [2, 1];
        CreateHandler(
                new
                {
                    Items = reversed,
                })
            .ShouldEmit(
                null,
                new(),
                new
                {
                    Items = ordered,
                });
        Assert.ThrowsAny<XunitException>(() => CreateHandler(
                new
                {
                    Items = extra,
                })
            .ShouldEmit(
                null,
                new(),
                new
                {
                    Items = ordered,
                }));
        Assert.ThrowsAny<XunitException>(() => CreateHandler(
                new
                {
                    Items = ordered,
                })
            .ShouldEmit(
                null,
                new(),
                new
                {
                    Items = duplicate,
                }));
        Assert.ThrowsAny<XunitException>(() => CreateHandler(
                new
                {
                    Bytes = reversedBytes,
                })
            .ShouldEmit(
                null,
                new(),
                new
                {
                    Bytes = orderedBytes,
                }));
    }

    /// <summary>
    ///     Unordered matches do not depend on which overlapping member subset is considered first.
    /// </summary>
    [Fact]
    public void UnorderedMemberSubsetsCanReassignMatches()
    {
        object[] expected =
        [
            new
            {
                Value = 1,
            },
            new
            {
                Value = 1,
                Extra = 2,
            },
        ];
        object[] actual =
        [
            new
            {
                Value = 1,
                Extra = 2,
            },
            new
            {
                Value = 1,
                Extra = 3,
            },
        ];
        CreateHandler(
                new
                {
                    Items = actual,
                })
            .ShouldEmit(
                null,
                new(),
                new
                {
                    Items = expected,
                });
    }
}