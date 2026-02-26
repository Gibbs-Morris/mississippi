using System.Diagnostics.CodeAnalysis;

using Mississippi.Inlet.Generators.Abstractions;


namespace Mississippi.Inlet.Runtime.L0Tests.Infrastructure;

/// <summary>
///     Type with authorization metadata but no ProjectionPath for scan tests.
/// </summary>
[GenerateAuthorization(Policy = "runtime.tests.policy")]
[SuppressMessage(
    "Performance",
    "CA1515:Consider making public types internal",
    Justification = "Must be public for GetExportedTypes() scanning.")]
public sealed record AuthorizationWithoutPathType(string Id);
