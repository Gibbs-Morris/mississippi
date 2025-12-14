using System;
using System.Linq;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.EventSourcing.Reducers.Abstractions;

using Xunit;


namespace Mississippi.EventSourcing.Reducers.L0Tests;

/// <summary>
///     Tests for ServiceRegistration extension methods.
/// </summary>
#pragma warning disable CA1707 // Test methods use underscore naming convention per repository standards
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1600 // Elements should be documented
public sealed class ServiceRegistrationTests
{
    [Fact]
    public void AddRootReducer_Should_ThrowArgumentNullException_GivenNullServices()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddRootReducer<TestModel>());
    }

    [Fact]
    public void AddRootReducer_Should_RegisterRootReducer_GivenValidServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRootReducer<TestModel>();

        // Assert
        ServiceDescriptor? descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IRootReducer<TestModel>)
        );
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddReducer_Should_ThrowArgumentNullException_GivenNullServices()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => services.AddReducer<TestModel, TestEvent, TestReducer>()
        );
    }

    [Fact]
    public void AddReducer_Should_RegisterReducer_GivenValidServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddReducer<TestModel, TestEvent, TestReducer>();

        // Assert
        ServiceDescriptor? typedDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IReducer<TestModel, TestEvent>)
        );
        Assert.NotNull(typedDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, typedDescriptor.Lifetime);

        ServiceDescriptor? objectDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IReducer<TestModel, object>)
        );
        Assert.NotNull(objectDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, objectDescriptor.Lifetime);
    }

    [Fact]
    public void AddReducer_Should_RegisterMultipleReducers_GivenMultipleRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddReducer<TestModel, TestEvent, TestReducer>();
        services.AddReducer<TestModel, OtherEvent, OtherReducer>();

        // Assert
        int objectReducerCount = services.Count(d => d.ServiceType == typeof(IReducer<TestModel, object>));
        Assert.Equal(2, objectReducerCount);
    }

    [Fact]
    public void AddReducer_Should_RegisterReducerInstance_GivenValidInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        IReducer<TestModel, TestEvent> reducer = new TestReducer();

        // Act
        services.AddReducer(reducer);

        // Assert
        ServiceDescriptor? typedDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IReducer<TestModel, TestEvent>)
        );
        Assert.NotNull(typedDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, typedDescriptor.Lifetime);
    }

    [Fact]
    public void AddReducer_Should_ThrowArgumentNullException_GivenNullReducerInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        IReducer<TestModel, TestEvent>? nullReducer = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => services.AddReducer(nullReducer!)
        );
    }

    [Fact]
    public void AddReducer_Should_RegisterReducerFactory_GivenValidFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddReducer<TestModel, TestEvent>(_ => new TestReducer());

        // Assert
        ServiceDescriptor? typedDescriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IReducer<TestModel, TestEvent>)
        );
        Assert.NotNull(typedDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, typedDescriptor.Lifetime);
    }

    [Fact]
    public void AddReducer_Should_ThrowArgumentNullException_GivenNullFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        Func<IServiceProvider, IReducer<TestModel, TestEvent>>? nullFactory = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => services.AddReducer(nullFactory!)
        );
    }

    [Fact]
    public void IntegrationTest_Should_ResolveRootReducer_GivenRegisteredReducers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddReducer<TestModel, TestEvent, TestReducer>();
        services.AddRootReducer<TestModel>();

        using ServiceProvider provider = services.BuildServiceProvider();

        // Act
        var rootReducer = provider.GetRequiredService<IRootReducer<TestModel>>();

        // Assert
        Assert.NotNull(rootReducer);
        Assert.IsType<RootReducer<TestModel>>(rootReducer);
    }

    [Fact]
    public void IntegrationTest_Should_ApplyEvents_GivenResolvedRootReducer()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddReducer<TestModel, TestEvent, TestReducer>();
        services.AddRootReducer<TestModel>();

        using ServiceProvider provider = services.BuildServiceProvider();
        var rootReducer = provider.GetRequiredService<IRootReducer<TestModel>>();
        var model = new TestModel { Value = 10 };
        var @event = new TestEvent { Delta = 5 };

        // Act
        TestModel result = rootReducer.Reduce(model, @event);

        // Assert
        Assert.Equal(15, result.Value);
    }

    private sealed record TestModel
    {
        public int Value { get; init; }
    }

    private sealed record TestEvent
    {
        public int Delta { get; init; }
    }

    private sealed record OtherEvent
    {
        public string Name { get; init; } = string.Empty;
    }

    private sealed class TestReducer : IReducer<TestModel, TestEvent>
    {
        public TestModel Reduce(
            TestModel model,
            TestEvent @event
        )
        {
            return model with { Value = model.Value + @event.Delta };
        }
    }

    private sealed class OtherReducer : IReducer<TestModel, OtherEvent>
    {
        public TestModel Reduce(
            TestModel model,
            OtherEvent @event
        )
        {
            return model;
        }
    }
}
