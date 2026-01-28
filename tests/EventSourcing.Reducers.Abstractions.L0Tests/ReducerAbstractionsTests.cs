using System;
using System.Reflection;



namespace Mississippi.EventSourcing.Reducers.Abstractions.L0Tests;

/// <summary>
///     Tests for event reducer abstractions.
/// </summary>
public sealed class ReducerAbstractionsTests
{
    private sealed record MutableEvent(string Value);

    private sealed class MutableProjection
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class MutatingEventReducer : EventReducerBase<MutableEvent, MutableProjection>
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

    private sealed class NonMutatingEventReducer : EventReducerBase<MutableEvent, MutableProjection>
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

    private sealed class NullProjectionEventReducer : EventReducerBase<MutableEvent, MutableProjection?>
    {
        protected override MutableProjection? ReduceCore(
            MutableProjection? state,
            MutableEvent eventData
        ) =>
            null;
    }

    /// <summary>
    ///     Verifies the projection-scoped event reducer contract shape stays stable.
    /// </summary>
    [Fact]
    public void IEventReducerShouldExposeTryReduceMethod()
    {
        Type reducerType = typeof(IEventReducer<>);
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
    ///     Verifies the root event reducer contract shape stays stable.
    /// </summary>
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
    [Fact]
    public void ReduceShouldPermitNullStateAndProjection()
    {
        NullProjectionEventReducer eventReducer = new();
        MutableProjection? projection = eventReducer.Reduce(null!, new("e2"));
        Assert.Null(projection);
    }

    /// <summary>
    ///     Verifies the base event reducer class is available for inheritance.
    /// </summary>
    [Fact]
    public void ReducerBaseClassShouldBeAbstractAndImplementIEventReducer()
    {
        Type reducerType = typeof(EventReducerBase<,>);
        Assert.True(reducerType.IsPublic);
        Assert.True(reducerType.IsClass);
        Assert.True(reducerType.IsAbstract);
        Assert.Contains(
            reducerType.GetInterfaces(),
            x => x.IsGenericType && (x.GetGenericTypeDefinition() == typeof(IEventReducer<>)));
        Assert.Contains(
            reducerType.GetInterfaces(),
            x => x.IsGenericType && (x.GetGenericTypeDefinition() == typeof(IEventReducer<,>)));
    }

    /// <summary>
    ///     Verifies the base event reducer guard prevents returning the same reference for reference types.
    /// </summary>
    [Fact]
    public void ReducerBaseClassShouldRejectMutatingReducerReturningSameInstance()
    {
        MutableProjection state = new()
        {
            Value = "s0",
        };
        MutatingEventReducer eventReducer = new();
        Assert.Throws<InvalidOperationException>(() => eventReducer.Reduce(state, new("e0")));
    }

    /// <summary>
    ///     Verifies TryReduce also enforces the immutability guard.
    /// </summary>
    [Fact]
    public void TryReduceShouldRejectMutatingReducerReturningSameInstance()
    {
        MutableProjection state = new()
        {
            Value = "s0",
        };
        MutatingEventReducer eventReducer = new();
        Assert.Throws<InvalidOperationException>(() => eventReducer.TryReduce(
            state,
            new MutableEvent("e0"),
            out MutableProjection _));
    }

    /// <summary>
    ///     Verifies TryReduce returns false and leaves state unchanged when the event type does not match.
    /// </summary>
    [Fact]
    public void TryReduceShouldReturnFalseWhenEventTypeDoesNotMatch()
    {
        MutableProjection state = new()
        {
            Value = "s0",
        };
        MutatingEventReducer eventReducer = new();
        bool reduced = eventReducer.TryReduce(state, new(), out MutableProjection projection);
        Assert.False(reduced);
        Assert.Null(projection);
        Assert.Equal("s0", state.Value);
    }

    /// <summary>
    ///     Verifies TryReduce returns the projection produced by ReduceCore when the event matches.
    /// </summary>
    [Fact]
    public void TryReduceShouldReturnProjectionWhenEventMatches()
    {
        MutableProjection state = new()
        {
            Value = "s0",
        };
        NonMutatingEventReducer eventReducer = new();
        bool reduced = eventReducer.TryReduce(state, new MutableEvent("e1"), out MutableProjection projection);
        Assert.True(reduced);
        Assert.NotSame(state, projection);
        Assert.Equal("s0-e1", projection.Value);
        Assert.Equal("s0", state.Value);
    }

    /// <summary>
    ///     Verifies the typed event reducer contract shape stays stable.
    /// </summary>
    [Fact]
    public void TypedIEventReducerShouldDeriveFromProjectionReducer()
    {
        Type reducerType = typeof(IEventReducer<,>);
        Assert.True(reducerType.IsPublic);
        Assert.True(reducerType.IsInterface);
        Assert.Contains(
            reducerType.GetInterfaces(),
            x => x.IsGenericType && (x.GetGenericTypeDefinition() == typeof(IEventReducer<>)));
    }
}