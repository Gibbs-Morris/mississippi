using System.Collections.Generic;

namespace Mississippi.Architecture.L0Tests;

/// <summary>
///     Documents known architectural violations that need remediation.
///     Each violation should have a corresponding backlog item.
/// </summary>
/// <remarks>
///     This file exists to track violations while keeping tests passing.
///     When a violation is fixed, remove it from the appropriate list and the test will start enforcing that type.
/// </remarks>
internal static class KnownViolations
{
    /// <summary>
    ///     Gets the abstraction types that depend on implementation types.
    ///     See abstractions-projects.instructions.md: "abstractions MUST NOT depend on implementations".
    /// </summary>
    internal static IReadOnlyList<string> AbstractionImplementationDependencies { get; } =
    [
        "ICosmosRepository",
        "ISnapshotContainerOperations"
    ];
}
