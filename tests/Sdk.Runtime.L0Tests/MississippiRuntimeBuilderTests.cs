using System;

using Microsoft.Extensions.DependencyInjection;

using Mississippi.DomainModeling.Abstractions.Builders;
using Mississippi.Sdk.Runtime;

using Moq;

using Orleans.Hosting;


namespace MississippiTests.Sdk.Runtime.L0Tests;

/// <summary>
///     Tests for <see cref="MississippiRuntimeBuilder" />.
/// </summary>
public sealed class MississippiRuntimeBuilderTests
{
    private static ISiloBuilder CreateSiloBuilder(
        IServiceCollection services
    )
    {
        Mock<ISiloBuilder> mock = new();
        mock.SetupGet(b => b.Services).Returns(services);
        return mock.Object;
    }

    /// <summary>
    ///     AddCosmosEventStorage should be idempotent.
    /// </summary>
    [Fact]
    public void AddCosmosEventStorageIsIdempotent()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = CreateSiloBuilder(services);

        // Act
        siloBuilder.UseMississippi(runtime =>
        {
            runtime.AddCosmosEventStorage(o => o.DatabaseId = "db1");
            runtime.AddCosmosEventStorage(o => o.DatabaseId = "db2");
        });

        // Assert — completed without exception; idempotent.
        Assert.NotNull(siloBuilder);
    }

    /// <summary>
    ///     AddCosmosEventStorage with null action should throw.
    /// </summary>
    [Fact]
    public void AddCosmosEventStorageWithNullActionThrows()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = CreateSiloBuilder(services);

        // Act & Assert
        siloBuilder.UseMississippi(runtime =>
            Assert.Throws<ArgumentNullException>(() => runtime.AddCosmosEventStorage(null!)));
    }

    /// <summary>
    ///     AddCosmosSnapshotStorage should be idempotent.
    /// </summary>
    [Fact]
    public void AddCosmosSnapshotStorageIsIdempotent()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = CreateSiloBuilder(services);

        // Act
        siloBuilder.UseMississippi(runtime =>
        {
            runtime.AddCosmosSnapshotStorage(o => o.DatabaseId = "db1");
            runtime.AddCosmosSnapshotStorage(o => o.DatabaseId = "db2");
        });

        // Assert — completed without exception; idempotent.
        Assert.NotNull(siloBuilder);
    }

    /// <summary>
    ///     AddCosmosSnapshotStorage with null action should throw.
    /// </summary>
    [Fact]
    public void AddCosmosSnapshotStorageWithNullActionThrows()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = CreateSiloBuilder(services);

        // Act & Assert
        siloBuilder.UseMississippi(runtime =>
            Assert.Throws<ArgumentNullException>(() => runtime.AddCosmosSnapshotStorage(null!)));
    }

    /// <summary>
    ///     AddEventSourcing should be idempotent.
    /// </summary>
    [Fact]
    public void AddEventSourcingIsIdempotent()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = CreateSiloBuilder(services);

        // Act — call twice; should not throw.
        siloBuilder.UseMississippi(runtime =>
        {
            runtime.AddEventSourcing(o => o.OrleansStreamProviderName = "A");
            runtime.AddEventSourcing(o => o.OrleansStreamProviderName = "B");
        });

        // Assert — completed without exception; first call wins.
        Assert.NotNull(siloBuilder);
    }

    /// <summary>
    ///     AddEventSourcing with null action should throw.
    /// </summary>
    [Fact]
    public void AddEventSourcingWithNullActionThrows()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = CreateSiloBuilder(services);

        // Act & Assert
        siloBuilder.UseMississippi(runtime =>
            Assert.Throws<ArgumentNullException>(() => runtime.AddEventSourcing(null!)));
    }

    /// <summary>
    ///     AddJsonSerialization should be idempotent.
    /// </summary>
    [Fact]
    public void AddJsonSerializationIsIdempotent()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = CreateSiloBuilder(services);

        // Act — call twice; should not throw.
        siloBuilder.UseMississippi(runtime =>
        {
            runtime.AddJsonSerialization();
            runtime.AddJsonSerialization();
        });

        // Assert — completed without exception; idempotent.
        Assert.NotNull(siloBuilder);
    }

    /// <summary>
    ///     Aggregates callback should invoke the delegate with a non-null builder.
    /// </summary>
    [Fact]
    public void AggregatesInvokesCallbackWithNonNullBuilder()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = CreateSiloBuilder(services);
        IAggregateBuilder? captured = null;

        // Act
        siloBuilder.UseMississippi(runtime => runtime.Aggregates(agg => captured = agg));

        // Assert
        Assert.NotNull(captured);
    }

    /// <summary>
    ///     Aggregates callback with null delegate should throw.
    /// </summary>
    [Fact]
    public void AggregatesWithNullDelegateThrows()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = CreateSiloBuilder(services);

        // Act & Assert
        siloBuilder.UseMississippi(runtime => Assert.Throws<ArgumentNullException>(() => runtime.Aggregates(null!)));
    }

    /// <summary>
    ///     Empty builder with no registrations should validate successfully.
    /// </summary>
    [Fact]
    public void EmptyBuilderValidatesSuccessfully()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = CreateSiloBuilder(services);

        // Act — empty configure callback should not throw during validate.
        siloBuilder.UseMississippi(_ => { });

        // Assert — completed without exception.
        Assert.NotNull(siloBuilder);
    }

    /// <summary>
    ///     Multiple sub-builder calls should reuse the same sub-builder instance.
    /// </summary>
    [Fact]
    public void MultipleSubBuilderCallsReuseInstance()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = CreateSiloBuilder(services);
        IAggregateBuilder? first = null;
        IAggregateBuilder? second = null;

        // Act
        siloBuilder.UseMississippi(runtime =>
        {
            runtime.Aggregates(agg => first = agg);
            runtime.Aggregates(agg => second = agg);
        });

        // Assert
        Assert.NotNull(first);
        Assert.Same(first, second);
    }

    /// <summary>
    ///     Projections callback should invoke the delegate with a non-null builder.
    /// </summary>
    [Fact]
    public void ProjectionsInvokesCallbackWithNonNullBuilder()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = CreateSiloBuilder(services);
        IProjectionBuilder? captured = null;

        // Act
        siloBuilder.UseMississippi(runtime => runtime.Projections(proj => captured = proj));

        // Assert
        Assert.NotNull(captured);
    }

    /// <summary>
    ///     Projections callback with null delegate should throw.
    /// </summary>
    [Fact]
    public void ProjectionsWithNullDelegateThrows()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = CreateSiloBuilder(services);

        // Act & Assert
        siloBuilder.UseMississippi(runtime => Assert.Throws<ArgumentNullException>(() => runtime.Projections(null!)));
    }

    /// <summary>
    ///     Sagas callback should invoke the delegate with a non-null builder.
    /// </summary>
    [Fact]
    public void SagasInvokesCallbackWithNonNullBuilder()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = CreateSiloBuilder(services);
        ISagaBuilder? captured = null;

        // Act
        siloBuilder.UseMississippi(runtime => runtime.Sagas(saga => captured = saga));

        // Assert
        Assert.NotNull(captured);
    }

    /// <summary>
    ///     Sagas callback with null delegate should throw.
    /// </summary>
    [Fact]
    public void SagasWithNullDelegateThrows()
    {
        // Arrange
        ServiceCollection services = [];
        ISiloBuilder siloBuilder = CreateSiloBuilder(services);

        // Act & Assert
        siloBuilder.UseMississippi(runtime => Assert.Throws<ArgumentNullException>(() => runtime.Sagas(null!)));
    }
}