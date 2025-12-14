using System;
using System.Reflection;

using Allure.Xunit.Attributes;


namespace Mississippi.EventSourcing.Reducers.Abstractions.L0Tests;

/// <summary>
///     Tests for reducer abstractions.
/// </summary>
public sealed class ReducerAbstractionsTests
{
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