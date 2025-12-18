using System;

using Moq;

using Orleans.Runtime;


namespace Mississippi.Testing.Utilities.Mocks;

/// <summary>
///     Fluent builder for creating <see cref="IGrainContext" /> mocks for Orleans grain testing.
/// </summary>
public sealed class GrainContextMockBuilder
{
    private readonly Mock<IGrainContext> mock = new();

    /// <summary>
    ///     Creates a new instance of the <see cref="GrainContextMockBuilder" />.
    /// </summary>
    /// <returns>A new builder instance.</returns>
    public static GrainContextMockBuilder Create() => new();

    /// <summary>
    ///     Builds and returns the configured mock.
    /// </summary>
    /// <returns>The configured <see cref="Mock{IGrainContext}" />.</returns>
    public Mock<IGrainContext> Build() => mock;

    /// <summary>
    ///     Builds and returns the mock object.
    /// </summary>
    /// <returns>The configured <see cref="IGrainContext" />.</returns>
    public IGrainContext BuildObject() => mock.Object;

    /// <summary>
    ///     Allows additional custom configuration of the underlying mock.
    /// </summary>
    /// <param name="configure">The configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    public GrainContextMockBuilder Configure(
        Action<Mock<IGrainContext>> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(mock);
        return this;
    }

    /// <summary>
    ///     Configures the mock to return a <see cref="GrainId" /> composed of a brook name and entity ID.
    /// </summary>
    /// <param name="brookName">The brook name (e.g., "TEST.AGGREGATES.TESTBROOK").</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <returns>The builder for chaining.</returns>
    public GrainContextMockBuilder WithBrookGrainKey(
        string brookName,
        string entityId
    ) =>
        WithGrainId("test", $"{brookName}|{entityId}");

    /// <summary>
    ///     Configures the mock to return the specified <see cref="GrainId" />.
    /// </summary>
    /// <param name="grainType">The grain type identifier.</param>
    /// <param name="grainKey">The grain key.</param>
    /// <returns>The builder for chaining.</returns>
    public GrainContextMockBuilder WithGrainId(
        string grainType,
        string grainKey
    )
    {
        GrainId grainId = GrainId.Create(grainType, grainKey);
        mock.Setup(c => c.GrainId).Returns(grainId);
        return this;
    }

    /// <summary>
    ///     Configures the mock to return a <see cref="GrainId" /> with "test" as the grain type.
    /// </summary>
    /// <param name="grainKey">The grain key.</param>
    /// <returns>The builder for chaining.</returns>
    public GrainContextMockBuilder WithGrainKey(
        string grainKey
    ) =>
        WithGrainId("test", grainKey);

    /// <summary>
    ///     Configures the mock to return a <see cref="GrainId" /> for snapshot grains.
    /// </summary>
    /// <param name="brookName">The brook name.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="reducersHash">The reducers hash.</param>
    /// <param name="position">The snapshot position.</param>
    /// <returns>The builder for chaining.</returns>
    public GrainContextMockBuilder WithSnapshotGrainKey(
        string brookName,
        string entityId,
        string reducersHash,
        long position
    ) =>
        WithGrainId("test", $"{brookName}|{entityId}|{reducersHash}|{position}");
}