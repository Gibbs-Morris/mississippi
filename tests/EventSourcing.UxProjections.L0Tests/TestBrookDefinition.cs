using Mississippi.EventSourcing.Abstractions;
using Mississippi.EventSourcing.Abstractions.Attributes;


namespace Mississippi.EventSourcing.UxProjections.L0Tests;

/// <summary>
///     Test brook definition for testing purposes.
/// </summary>
/// <remarks>
///     This type is internal but accessible to Moq via the InternalsVisibleTo attribute
///     for DynamicProxyGenAssembly2 configured in Directory.Build.props.
/// </remarks>
[BrookName("TEST", "MODULE", "STREAM")]
internal sealed class TestBrookDefinition : IBrookDefinition
{
    /// <inheritdoc />
    public static string BrookName => "TEST.MODULE.STREAM";
}