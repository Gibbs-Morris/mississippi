using Mississippi.Brooks.Abstractions.Attributes;
using Mississippi.DomainModeling.Abstractions.Aggregates.ReferenceSet.State;

using MississippiTests.DomainModeling.Runtime.L0Tests.Projections.SavedItemsMembership;

using Orleans;


namespace MississippiTests.DomainModeling.Runtime.L0Tests.Projections.ReferenceSetPages;

/// <summary>Provides a second named projection/source pair with an explicit nondefault snapshot schema.</summary>
[BrookName("TEST", "REFERENCESET", "FOLLOWEDLISTS")]
[SnapshotStorageName("TEST", "REFERENCESET", "FOLLOWEDLISTS", 2)]
[GenerateSerializer]
[Alias(
    "MississippiTests.DomainModeling.Runtime.L0Tests.Projections.ReferenceSetPages.FollowedListsMembershipProjection")]
internal sealed record FollowedListsMembershipProjection
{
    /// <summary>Gets references to saved membership projections without loading their contents.</summary>
    [Id(0)]
    public ReferenceSetState<SavedItemsMembershipProjection> References { get; init; } = new();
}