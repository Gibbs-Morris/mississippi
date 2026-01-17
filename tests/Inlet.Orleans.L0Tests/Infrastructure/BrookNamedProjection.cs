using System.Diagnostics.CodeAnalysis;

using Mississippi.EventSourcing.Brooks.Abstractions.Attributes;
using Mississippi.Inlet.Projection.Abstractions;


namespace Mississippi.Inlet.Orleans.L0Tests.Infrastructure;

/// <summary>
///     A test projection with both ProjectionPathAttribute and BrookNameAttribute.
/// </summary>
/// <param name="Id">The projection identifier.</param>
/// <remarks>
///     This type must be public for <see cref="System.Reflection.Assembly.GetExportedTypes" />
///     to discover it during assembly scanning tests.
/// </remarks>
[ProjectionPath("/api/brook-named-projection")]
[BrookName("TEST", "MODULE", "BROOKNAME")]
[SuppressMessage(
    "Performance",
    "CA1515:Consider making public types internal",
    Justification = "Must be public for GetExportedTypes() scanning.")]
public sealed record BrookNamedProjection(string Id);