namespace MississippiTests.DomainModeling.ReplicaSinks.Runtime.L0Tests;

/// <summary>
///     Test projection used by replica-sink latest-state processor tests.
/// </summary>
/// <remarks>
///     This type is internal but accessible to Moq via the InternalsVisibleTo attribute
///     for DynamicProxyGenAssembly2 configured in Directory.Build.props.
/// </remarks>
internal sealed class TestProjection
{
    /// <summary>
    ///     Gets the test identifier.
    /// </summary>
    public string Id { get; init; } = string.Empty;
}