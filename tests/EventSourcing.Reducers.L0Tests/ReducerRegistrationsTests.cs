using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Reducers.Abstractions;


namespace Mississippi.EventSourcing.Reducers.L0Tests;

/// <summary>
///     Tests for event reducer registrations.
/// </summary>
public sealed class ReducerRegistrationsTests
{
    private sealed record TestEvent(string Value);

    private sealed class TestEventReducer : IEventReducer<TestEvent, TestProjection>
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

    private sealed record TestProjection(string Value);

    /// <summary>
    ///     Verifies the delegate-based AddReducer overload registers the event reducer and root event reducer.
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
        IEventReducer<TestEvent, TestProjection> eventReducer =
            provider.GetRequiredService<IEventReducer<TestEvent, TestProjection>>();
        TestProjection projection = eventReducer.Reduce(new("s0"), new("v1"));
        Assert.Equal("s0-v1", projection.Value);
        IRootReducer<TestProjection> root = provider.GetRequiredService<IRootReducer<TestProjection>>();
        TestProjection state = new("s0");
        TestProjection reduced = root.Reduce(state, new TestEvent("v2"));
        Assert.Equal("s0-v2", reduced.Value);
    }

    /// <summary>
    ///     Verifies AddReducer registers the event reducer implementation as a transient service.
    /// </summary>
    [Fact]
    public void AddReducerShouldRegisterReducerAsTransient()
    {
        ServiceCollection services = new();
        services.AddReducer<TestEvent, TestProjection, TestEventReducer>();
        ServiceDescriptor projectionDescriptor = services.Single(x =>
            x.ServiceType == typeof(IEventReducer<TestProjection>));
        Assert.Equal(ServiceLifetime.Transient, projectionDescriptor.Lifetime);
        Assert.Equal(typeof(TestEventReducer), projectionDescriptor.ImplementationType);
        Assert.Null(projectionDescriptor.ImplementationFactory);
        Assert.Null(projectionDescriptor.ImplementationInstance);
        ServiceDescriptor typedDescriptor = services.Single(x =>
            x.ServiceType == typeof(IEventReducer<TestEvent, TestProjection>));
        Assert.Equal(ServiceLifetime.Transient, typedDescriptor.Lifetime);
        Assert.Equal(typeof(TestEventReducer), typedDescriptor.ImplementationType);
        Assert.Null(typedDescriptor.ImplementationFactory);
        Assert.Null(typedDescriptor.ImplementationInstance);
    }

    /// <summary>
    ///     Verifies AddReducer also registers a root event reducer for the projection type when missing.
    /// </summary>
    [Fact]
    public void AddReducerShouldRegisterRootReducerWhenMissing()
    {
        ServiceCollection services = new();
        services.AddReducer<TestEvent, TestProjection, TestEventReducer>();
        ServiceDescriptor rootDescriptor = services.Single(x => x.ServiceType == typeof(IRootReducer<TestProjection>));
        Assert.Equal(ServiceLifetime.Transient, rootDescriptor.Lifetime);
        Assert.Equal(typeof(RootReducer<TestProjection>), rootDescriptor.ImplementationType);
        Assert.Null(rootDescriptor.ImplementationFactory);
        Assert.Null(rootDescriptor.ImplementationInstance);
    }

    /// <summary>
    ///     Verifies AddRootReducer registers the root event reducer implementation as a transient service.
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