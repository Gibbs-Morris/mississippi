using System.Diagnostics.CodeAnalysis;

using Mississippi.Inlet.Abstractions;


namespace Mississippi.Inlet.Silo.L0Tests.Infrastructure;

/// <summary>
///     A test projection with only ProjectionPathAttribute.
/// </summary>
/// <param name="Id">The projection identifier.</param>
/// <remarks>
///     This type must be public for <see cref="System.Reflection.Assembly.GetExportedTypes" />
///     to discover it during assembly scanning tests.
/// </remarks>
[ProjectionPath("/api/path-only-projection")]
[SuppressMessage(
    "Performance",
    "CA1515:Consider making public types internal",
    Justification = "Must be public for GetExportedTypes() scanning.")]
public sealed record PathOnlyProjection(string Id);
