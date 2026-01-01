using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;


namespace Mississippi.EventSourcing.UxProjections.Api.L0Tests;

/// <summary>
///     Test grain type for testing purposes.
/// </summary>
/// <remarks>
///     This type is internal but accessible to Moq via the InternalsVisibleTo attribute
///     for DynamicProxyGenAssembly2 configured in Directory.Build.props.
/// </remarks>
[BrookName("TEST", "MODULE", "STREAM")]
internal sealed class TestGrain
{
}