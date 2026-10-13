using Mississippi.Brooks.Abstractions.Attributes;

using Orleans;


namespace Mississippi.DomainModeling.Runtime.L0Tests.Diagnostics;

/// <summary>
///     Isolates captured-latest query measurements from other projection tests.
/// </summary>
/// <param name="Value">The sample state value.</param>
[BrookName("TEST", "MODULE", "CAPTUREDMETRIC")]
[SnapshotStorageName("TEST", "UXPROJECTIONS", "CAPTUREDMETRIC")]
[GenerateSerializer]
[Alias("Mississippi.DomainModeling.Runtime.L0Tests.Diagnostics.CapturedLatestMetricProjection")]
internal sealed record CapturedLatestMetricProjection([property: Id(0)] int Value);