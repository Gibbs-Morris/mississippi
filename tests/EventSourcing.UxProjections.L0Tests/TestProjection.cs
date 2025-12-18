namespace Mississippi.EventSourcing.UxProjections.L0Tests;

/// <summary>
///     Test projection class for testing purposes.
/// </summary>
/// <param name="Value">The sample value.</param>
/// <remarks>
///     This type is internal but accessible to Moq via the InternalsVisibleTo attribute
///     for DynamicProxyGenAssembly2 configured in Directory.Build.props.
/// </remarks>
internal sealed record TestProjection(int Value);
