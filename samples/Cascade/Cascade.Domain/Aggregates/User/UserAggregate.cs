using System;
using System.Collections.Immutable;

using Mississippi.EventSourcing.Aggregates.Abstractions;
using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.Sdk.Generators.Abstractions;

using Orleans;


namespace Cascade.Domain.Aggregates.User;

/// <summary>
///     Internal state for the user aggregate.
///     This is never exposed externally; use projections for read queries.
/// </summary>
[BrookName("CASCADE", "CHAT", "USER")]
[AggregateService("users")]
[SnapshotStorageName("CASCADE", "CHAT", "USERSTATE")]
[GenerateAggregateEndpoints]
[GenerateSerializer]
[Alias("Cascade.Domain.User.UserAggregate")]
internal sealed record UserAggregate
{
    /// <summary>
    ///     Gets the set of channel IDs the user has joined.
    /// </summary>
    [Id(4)]
    public ImmutableHashSet<string> ChannelIds { get; init; } = [];

    /// <summary>
    ///     Gets the user's display name.
    /// </summary>
    [Id(2)]
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    ///     Gets a value indicating whether the user is currently online.
    /// </summary>
    [Id(3)]
    public bool IsOnline { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the user has been registered.
    /// </summary>
    [Id(0)]
    public bool IsRegistered { get; init; }

    /// <summary>
    ///     Gets the timestamp when the user was last seen.
    /// </summary>
    [Id(6)]
    public DateTimeOffset? LastSeenAt { get; init; }

    /// <summary>
    ///     Gets the timestamp when the user registered.
    /// </summary>
    [Id(5)]
    public DateTimeOffset RegisteredAt { get; init; }

    /// <summary>
    ///     Gets the user's unique identifier.
    /// </summary>
    [Id(1)]
    public string UserId { get; init; } = string.Empty;
}