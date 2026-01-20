namespace Mississippi.EventSourcing.UxProjections.Api.L0Tests;

/// <summary>
///     Test DTO class for testing purposes.
/// </summary>
/// <param name="Name">The sample name.</param>
/// <remarks>
///     This type is internal but accessible to Moq via the InternalsVisibleTo attribute
///     for DynamicProxyGenAssembly2 configured in Directory.Build.props.
/// </remarks>
internal sealed record TestDto(string Name);