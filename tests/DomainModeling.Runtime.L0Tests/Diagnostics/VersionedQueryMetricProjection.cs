using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.DomainModeling.Runtime.L0Tests.Diagnostics;

/// <summary>
///     Isolates query measurements from other projection tests.
/// </summary>
/// <param name="Value">The sample state value.</param>
[BrookName("TEST", "MODULE", "QUERYMETRIC")]
[SnapshotStorageName("TEST", "UXPROJECTIONS", "QUERYMETRIC", 2)]
[GenerateSerializer]
[Alias("Mississippi.DomainModeling.Runtime.L0Tests.Diagnostics.VersionedQueryMetricProjection")]
internal sealed record VersionedQueryMetricProjection([property: Id(0)] int Value);