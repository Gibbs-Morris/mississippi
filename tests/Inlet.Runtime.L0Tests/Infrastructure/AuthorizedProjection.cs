using System.Diagnostics.CodeAnalysis;

using Mississippi.Inlet.Abstractions;
using Mississippi.Inlet.Generators.Abstractions;


namespace Mississippi.Inlet.Runtime.L0Tests.Infrastructure;

/// <summary>
///     Projection with GenerateAuthorization metadata for assembly scan tests.
/// </summary>
[ProjectionPath("/api/authorized-projection")]
[GenerateAuthorization(Policy = "runtime.tests.policy", Roles = "reader", AuthenticationSchemes = "Bearer")]
[SuppressMessage(
    "Performance",
    "CA1515:Consider making public types internal",
    Justification = "Must be public for GetExportedTypes() scanning.")]
public sealed record AuthorizedProjection(string Id);