using System;
using System.Linq;

using Allure.Xunit.Attributes;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Mississippi.EventSourcing.Reducers.L0Tests;

/// <summary>
///     Tests for reducer registrations.
/// </summary>
[AllureParentSuite("Event Sourcing")]
[AllureSuite("Reducers")]
[AllureSubSuite("Reducer Registrations")]
public sealed class ReducerRegistrationsTests
{
    private sealed record TestEvent(string Value);

    private sealed record TestProjection(string Value);

    private sealed class TestReducer : IReducer<TestEvent, TestProjection>
    {
        public TestProjection Reduce(
            TestProjection state,
            TestEvent eventData
        )
        {
            ArgumentNullException.ThrowIfNull(eventData);
            return new($"{state.Value}-{eventData.Value}");
        }

        public bool TryReduce(
            TestProjection state,
            object eventData,
            out TestProjection projection
        )
        {
            ArgumentNullException.ThrowIfNull(eventData);
            if (eventData is not TestEvent typedEvent)
            {
                projection = default!;
                return false;
            }

            projection = Reduce(state, typedEvent);
            return true;
        }
    }

    /// <summary>
    ///     Verifies the delegate-based AddReducer overload registers the reducer and root reducer.
    /// </summary>
    [Fact]
    public void AddReducerDelegateOverloadShouldRegisterReducerAndRootReducer()
    {
        ServiceCollection services = new();
        services.AddReducer<TestEvent, TestProjection>((
            state,
            e
        ) => new($"{state.Value}-{e.Value}"));
        using ServiceProvider provider = services.BuildServiceProvider();
        IReducer<TestEvent, TestProjection>
            reducer = provider.GetRequiredService<IReducer<TestEvent, TestProjection>>();
        TestProjection projection = reducer.Reduce(new("s0"), new("v1"));
        Assert.Equal("s0-v1", projection.Value);
        IRootReducer<TestProjection> root = provider.GetRequiredService<IRootReducer<TestProjection>>();
        TestProjection state = new("s0");
        TestProjection reduced = root.Reduce(state, new TestEvent("v2"));
        Assert.Equal("s0-v2", reduced.Value);
    }

    /// <summary>
    ///     Verifies AddReducer registers the reducer implementation as a transient service.
    /// </summary>
    [Fact]
    public void AddReducerShouldRegisterReducerAsTransient()
    {
        ServiceCollection services = new();
        services.AddReducer<TestEvent, TestProjection, TestReducer>();
        ServiceDescriptor projectionDescriptor = services.Single(x =>
            x.ServiceType == typeof(IReducer<TestProjection>));
        Assert.Equal(ServiceLifetime.Transient, projectionDescriptor.Lifetime);
        Assert.Equal(typeof(TestReducer), projectionDescriptor.ImplementationType);
        Assert.Null(projectionDescriptor.ImplementationFactory);
        Assert.Null(projectionDescriptor.ImplementationInstance);
        ServiceDescriptor typedDescriptor = services.Single(x =>
            x.ServiceType == typeof(IReducer<TestEvent, TestProjection>));
        Assert.Equal(ServiceLifetime.Transient, typedDescriptor.Lifetime);
        Assert.Equal(typeof(TestReducer), typedDescriptor.ImplementationType);
        Assert.Null(typedDescriptor.ImplementationFactory);
        Assert.Null(typedDescriptor.ImplementationInstance);
    }

    /// <summary>
    ///     Verifies AddReducer also registers a root reducer for the projection type when missing.
    /// </summary>
    [Fact]
    public void AddReducerShouldRegisterRootReducerWhenMissing()
    {
        ServiceCollection services = new();
        services.AddReducer<TestEvent, TestProjection, TestReducer>();
        ServiceDescriptor rootDescriptor = services.Single(x => x.ServiceType == typeof(IRootReducer<TestProjection>));
        Assert.Equal(ServiceLifetime.Transient, rootDescriptor.Lifetime);
        Assert.Equal(typeof(RootReducer<TestProjection>), rootDescriptor.ImplementationType);
        Assert.Null(rootDescriptor.ImplementationFactory);
        Assert.Null(rootDescriptor.ImplementationInstance);
    }

    /// <summary>
    ///     Verifies AddRootReducer registers the root reducer implementation as a transient service.
    /// </summary>
    [Fact]
    public void AddRootReducerShouldRegisterRootReducerAsTransient()
    {
        ServiceCollection services = new();
        services.AddRootReducer<TestProjection>();
        ServiceDescriptor descriptor = services.Single(x => x.ServiceType == typeof(IRootReducer<TestProjection>));
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
        Assert.Equal(typeof(RootReducer<TestProjection>), descriptor.ImplementationType);
        Assert.Null(descriptor.ImplementationFactory);
        Assert.Null(descriptor.ImplementationInstance);
    }
}