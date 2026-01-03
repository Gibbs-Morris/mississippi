namespace Mississippi.Ripples.Blazor.L0Tests;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Test projection record for unit tests.
/// </summary>
/// <param name="Name">The name value.</param>
/// <param name="Value">The numeric value.</param>
[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required for Razor component parameters.")]
public sealed record TestProjection(string Name, int Value);
