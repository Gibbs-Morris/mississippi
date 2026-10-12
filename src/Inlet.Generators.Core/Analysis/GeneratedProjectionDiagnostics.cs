using Microsoft.CodeAnalysis;


namespace Mississippi.Inlet.Generators.Core.Analysis;

/// <summary>
///     Diagnostics for generated projection DTO and mapper contracts.
/// </summary>
public static class GeneratedProjectionDiagnostics
{
    /// <summary>
    ///     Reports an enum collection without a compatible DTO materializer.
    /// </summary>
    public static readonly DiagnosticDescriptor UnsupportedEnumCollection = new(
        "INLETDTO002",
        "Unsupported enum collection shape",
        "Property '{0}' uses enum collection '{1}' without a compatible DTO materializer. Use a one-dimensional array or a supported list or set collection.",
        "Mississippi.Inlet.Projections",
        DiagnosticSeverity.Error,
        true);
}