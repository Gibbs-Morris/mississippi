using System;
using System.Reflection;

using Allure.Xunit.Attributes;


namespace Mississippi.EventSourcing.Reducers.Abstractions.L0Tests;

/// <summary>
///     Tests for reducer abstractions.
/// </summary>
public sealed class ReducerAbstractionsTests
{
    private sealed record MutableEvent(string Value);

    private sealed class MutableProjection
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class MutatingReducer : Reducer<MutableEvent, MutableProjection>
    {
        protected override MutableProjection ReduceCore(
            MutableProjection state,
            MutableEvent eventData
        )
        {
            state.Value = eventData.Value;
            return state;
        }
    }

    private sealed class NonMutatingReducer : Reducer<MutableEvent, MutableProjection>
    {
        protected override MutableProjection ReduceCore(
            MutableProjection state,
            MutableEvent eventData
        ) =>
            new()
            {
                Value = $"{state.Value}-{eventData.Value}",
            };
    }

    private sealed class NullProjectionReducer : Reducer<MutableEvent, MutableProjection?>
    {
        protected override MutableProjection? ReduceCore(
            MutableProjection? state,
            MutableEvent eventData
        ) =>
            null;
    }

    /// <summary>
    ///     Verifies the projection-scoped reducer contract shape stays stable.
    /// </summary>
    [AllureEpic("Reducers")]
    [Fact]
    public void IReducerShouldExposeTryReduceMethod()
    {
        Type reducerType = typeof(IReducer<>);
        Assert.True(reducerType.IsPublic);
        Assert.True(reducerType.IsInterface);
        MethodInfo? reduceMethod = reducerType.GetMethod("TryReduce", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(reduceMethod);
        ParameterInfo[] parameters = reduceMethod!.GetParameters();
        Assert.Equal(3, parameters.Length);
        Assert.True(parameters[0].ParameterType.IsGenericParameter);
        Assert.Equal(typeof(object), parameters[1].ParameterType);
        Assert.True(parameters[2].IsOut);
        Assert.True(parameters[2].ParameterType.IsByRef);
        Assert.True(parameters[2].ParameterType.GetElementType()!.IsGenericParameter);
        Assert.Equal(typeof(bool), reduceMethod.ReturnType);
    }

    /// <summary>
    ///     Verifies the root reducer contract shape stays stable.
    /// </summary>
    [AllureEpic("Reducers")]
    [Fact]
    public void IRootReducerShouldExposeHashAndReduceMethods()
    {
        Type rootReducerType = typeof(IRootReducer<>);
        Assert.True(rootReducerType.IsPublic);
        Assert.True(rootReducerType.IsInterface);
        MethodInfo? getReducerHashMethod = rootReducerType.GetMethod(
            "GetReducerHash",
            BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(getReducerHashMethod);
        Assert.Equal(typeof(string), getReducerHashMethod!.ReturnType);
        MethodInfo? reduceMethod = rootReducerType.GetMethod("Reduce", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(reduceMethod);
        ParameterInfo[] reduceParameters = reduceMethod!.GetParameters();
        Assert.Equal(2, reduceParameters.Length);
        Assert.True(reduceParameters[0].ParameterType.IsGenericParameter);
        Assert.Equal(typeof(object), reduceParameters[1].ParameterType);
        Assert.True(reduceMethod.ReturnType.IsGenericParameter);
    }

    /// <summary>
    ///     Verifies reducers may legitimately return null when both state and projection are null.
    /// </summary>
    [AllureEpic("Reducers")]
    [Fact]
    public void ReduceShouldPermitNullStateAndProjection()
    {
        NullProjectionReducer reducer = new();
        MutableProjection? projection = reducer.Reduce(null!, new("e2"));
        Assert.Null(projection);
    }

    /// <summary>
    ///     Verifies the base reducer class is available for inheritance.
    /// </summary>
    [AllureEpic("Reducers")]
    [Fact]
    public void ReducerBaseClassShouldBeAbstractAndImplementIReducer()
    {
        Type reducerType = typeof(Reducer<,>);
        Assert.True(reducerType.IsPublic);
        Assert.True(reducerType.IsClass);
        Assert.True(reducerType.IsAbstract);
        Assert.Contains(
            reducerType.GetInterfaces(),
            x => x.IsGenericType && (x.GetGenericTypeDefinition() == typeof(IReducer<>)));
        Assert.Contains(
            reducerType.GetInterfaces(),
            x => x.IsGenericType && (x.GetGenericTypeDefinition() == typeof(IReducer<,>)));
    }

    /// <summary>
    ///     Verifies the base reducer guard prevents returning the same reference for reference types.
    /// </summary>
    [AllureEpic("Reducers")]
    [Fact]
    public void ReducerBaseClassShouldRejectMutatingReducerReturningSameInstance()
    {
        MutableProjection state = new()
        {
            Value = "s0",
        };
        MutatingReducer reducer = new();
        Assert.Throws<InvalidOperationException>(() => reducer.Reduce(state, new("e0")));
    }

    /// <summary>
    ///     Verifies TryReduce also enforces the immutability guard.
    /// </summary>
    [AllureEpic("Reducers")]
    [Fact]
    public void TryReduceShouldRejectMutatingReducerReturningSameInstance()
    {
        MutableProjection state = new()
        {
            Value = "s0",
        };
        MutatingReducer reducer = new();
        Assert.Throws<InvalidOperationException>(() => reducer.TryReduce(
            state,
            new MutableEvent("e0"),
            out MutableProjection _));
    }

    /// <summary>
    ///     Verifies TryReduce returns false and leaves state unchanged when the event type does not match.
    /// </summary>
    [AllureEpic("Reducers")]
    [Fact]
    public void TryReduceShouldReturnFalseWhenEventTypeDoesNotMatch()
    {
        MutableProjection state = new()
        {
            Value = "s0",
        };
        MutatingReducer reducer = new();
        bool reduced = reducer.TryReduce(state, new(), out MutableProjection projection);
        Assert.False(reduced);
        Assert.Null(projection);
        Assert.Equal("s0", state.Value);
    }

    /// <summary>
    ///     Verifies TryReduce returns the projection produced by ReduceCore when the event matches.
    /// </summary>
    [AllureEpic("Reducers")]
    [Fact]
    public void TryReduceShouldReturnProjectionWhenEventMatches()
    {
        MutableProjection state = new()
        {
            Value = "s0",
        };
        NonMutatingReducer reducer = new();
        bool reduced = reducer.TryReduce(state, new MutableEvent("e1"), out MutableProjection projection);
        Assert.True(reduced);
        Assert.NotSame(state, projection);
        Assert.Equal("s0-e1", projection.Value);
        Assert.Equal("s0", state.Value);
    }

    /// <summary>
    ///     Verifies the typed reducer contract shape stays stable.
    /// </summary>
    [AllureEpic("Reducers")]
    [Fact]
    public void TypedIReducerShouldDeriveFromProjectionReducer()
    {
        Type reducerType = typeof(IReducer<,>);
        Assert.True(reducerType.IsPublic);
        Assert.True(reducerType.IsInterface);
        Assert.Contains(
            reducerType.GetInterfaces(),
            x => x.IsGenericType && (x.GetGenericTypeDefinition() == typeof(IReducer<>)));
    }
}