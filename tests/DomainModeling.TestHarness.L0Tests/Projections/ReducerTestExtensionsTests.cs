using System;
using System.Collections.Generic;

using Mississippi.DomainModeling.TestHarness.Projections;
using Mississippi.Tributary.Abstractions;

using Moq;

using Xunit.Sdk;


namespace Mississippi.DomainModeling.TestHarness.L0Tests.Projections;

/// <summary>
///     Verifies the direct reducer assertions offered to test authors.
/// </summary>
public sealed class ReducerTestExtensionsTests
{
    /// <summary>
    ///     Apply and equality assertions should use explicit or default state and reject incorrect expectations.
    /// </summary>
    [Fact]
    public void ApplyShouldUseSuppliedOrDefaultState()
    {
        AppendTextReducer reducer = new();
        Assert.Equal(["first"], reducer.Apply(null, "first"));
        Assert.Equal(["initial", "second"], reducer.Apply(["initial"], "second"));
        reducer.ShouldProduce(null, "first", ["first"]);
        Assert.Throws<XunitException>(() => reducer.ShouldProduce(null, "first", ["wrong"]));
    }

    /// <summary>
    ///     Missing events must be rejected before invoking a custom reducer that accepts null.
    /// </summary>
    [Fact]
    public void ApplyShouldValidateEventBeforeCallingCustomReducer()
    {
        Mock<IEventReducer<string, List<string>>> reducer = new();
        reducer.Setup(value => value.Reduce(It.IsAny<List<string>>(), It.IsAny<string>())).Returns([]);
        Assert.Throws<ArgumentNullException>("eventData", () => reducer.Object.Apply(null, null!));
        reducer.Verify(value => value.Reduce(It.IsAny<List<string>>(), It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    ///     Reducer assertion helpers should reject missing reducer, event, and expected state.
    /// </summary>
    [Fact]
    public void AssertionHelpersShouldRejectNullInputs()
    {
        AppendTextReducer reducer = new();
        Assert.Throws<ArgumentNullException>(() =>
            ReducerTestExtensions.Apply<string, List<string>>(null!, null, "event"));
        Assert.Throws<ArgumentNullException>(() => reducer.Apply(null, null!));
        Assert.Throws<ArgumentNullException>(() => reducer.ShouldProduce(null, "event", null!));
        Assert.Throws<ArgumentNullException>(() =>
            ReducerTestExtensions.ShouldThrow<InvalidOperationException, string, List<string>>(null!, null, "event"));
    }

    /// <summary>
    ///     Exception assertions must execute against the caller's state rather than an empty projection.
    /// </summary>
    [Fact]
    public void ExceptionAssertionsShouldPreserveInitialState()
    {
        List<string> state = ["existing"];
        Mock<IEventReducer<string, List<string>>> reducer = new();
        reducer.Setup(value => value.Reduce(state, "invalid"))
            .Throws(new InvalidOperationException("Existing state rejected."));
        reducer.Object.ShouldThrow<InvalidOperationException, string, List<string>>(state, "invalid", "Existing state");
        reducer.Verify(value => value.Reduce(state, "invalid"), Times.Once);
    }

    /// <summary>
    ///     Exception assertions should check the exception type and optional message fragment.
    /// </summary>
    [Fact]
    public void ExceptionAssertionsShouldVerifyTypeAndMessage()
    {
        Mock<IEventReducer<string, List<string>>> reducer = new();
        reducer.Setup(value => value.Reduce(It.IsAny<List<string>>(), "invalid"))
            .Throws(new InvalidOperationException("Unsupported event payload."));
        reducer.Object.ShouldThrow<InvalidOperationException, string, List<string>>(null, "invalid");
        reducer.Object.ShouldThrow<InvalidOperationException, string, List<string>>(null, "invalid", "event payload");
        Assert.Throws<XunitException>(() =>
            reducer.Object.ShouldThrow<InvalidOperationException, string, List<string>>(null, "invalid", "wrong"));
        Assert.Throws<XunitException>(() =>
            reducer.Object.ShouldThrow<ArgumentException, string, List<string>>(null, "invalid"));
    }
}