using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Mississippi.EventSourcing.Reducers.Tests;

/// <summary>
///     Exercises the public service registration surface for reducers.
/// </summary>
public sealed class ServiceRegistrationTests
{
    private sealed class MultiReducer
        : IReducer<NumberModel>,
          IReducer<TextModel>
    {
        NumberModel IReducer<NumberModel>.Reduce(
            NumberModel input,
            object eventData
        )
        {
            if (eventData is not int delta)
            {
                return input;
            }

            return input with
            {
                Value = input.Value + delta,
            };
        }

        TextModel IReducer<TextModel>.Reduce(
            TextModel input,
            object eventData
        )
        {
            if (eventData is not string text)
            {
                return input;
            }

            return input with
            {
                Value = input.Value + text,
            };
        }
    }

    private sealed class NonReducer
    {
        public int Sentinel { get; } = 42;
    }

    private sealed record NumberModel(int Value);

    private sealed class TestReducer : IReducer<NumberModel>
    {
        public NumberModel Reduce(
            NumberModel input,
            object eventData
        )
        {
            if (eventData is not int delta)
            {
                return input;
            }

            return input with
            {
                Value = input.Value + delta,
            };
        }
    }

    private sealed record TextModel(string Value);

    /// <summary>
    ///     Ensures the feature-level registration wires the default root reducer.
    /// </summary>
    [Fact]
    public void AddEventSourcingReducersRegistersRootReducer()
    {
        ServiceCollection services = new();
        services.AddEventSourcingReducers();
        using ServiceProvider provider = services.BuildServiceProvider();
        IRootReducer<NumberModel> reducer = provider.GetRequiredService<IRootReducer<NumberModel>>();
        Assert.IsType<RootReducer<NumberModel>>(reducer);
    }

    /// <summary>
    ///     Ensures reducers registered with explicit model types are resolved as singletons.
    /// </summary>
    [Fact]
    public void AddReducerWithModelTypeRegistersReducer()
    {
        ServiceCollection services = new();
        services.AddReducer<TestReducer, NumberModel>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IReducer<NumberModel> reducer = provider.GetRequiredService<IReducer<NumberModel>>();
        Assert.IsType<TestReducer>(reducer);
    }

    /// <summary>
    ///     Ensures reducers registered via reflection bind to all supported model types.
    /// </summary>
    [Fact]
    public void AddReducerWithoutModelTypeRegistersAllImplementedInterfaces()
    {
        ServiceCollection services = new();
        services.AddReducer<MultiReducer>();
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.IsType<MultiReducer>(provider.GetRequiredService<IReducer<NumberModel>>());
        Assert.IsType<MultiReducer>(provider.GetRequiredService<IReducer<TextModel>>());
    }

    /// <summary>
    ///     Ensures an informative exception is thrown when the type lacks reducer interfaces.
    /// </summary>
    [Fact]
    public void AddReducerWithoutReducerInterfaceThrowsInvalidOperation()
    {
        ServiceCollection services = new();
        Assert.Throws<InvalidOperationException>(() => services.AddReducer<NonReducer>());
        Assert.Equal(42, new NonReducer().Sentinel);
    }

    /// <summary>
    ///     Ensures the legacy registration delegates to the feature-level overload.
    /// </summary>
    [Fact]
    public void AddRootReducerDelegatesToAddEventSourcingReducers()
    {
        ServiceCollection services = new();
        services.AddRootReducer<NumberModel>();
        using ServiceProvider provider = services.BuildServiceProvider();
        IRootReducer<NumberModel> reducer = provider.GetRequiredService<IRootReducer<NumberModel>>();
        Assert.IsType<RootReducer<NumberModel>>(reducer);
    }
}