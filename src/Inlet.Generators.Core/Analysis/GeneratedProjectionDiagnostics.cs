using Microsoft.CodeAnalysis;


namespace Mississippi.Inlet.Generators.Core.Analysis;

/// <summary>
///     Diagnostics for generated projection DTO and mapper contracts.
/// </summary>
public static class GeneratedProjectionDiagnostics
{
    /// <summary>
    ///     Reports a nullable enum collection without a compatible DTO materializer.
    /// </summary>
    public static readonly DiagnosticDescriptor UnsupportedNullableEnumCollection = new(
        "INLETDTO002",
        "Unsupported nullable enum collection shape",
        "Property '{0}' uses nullable enum collection '{1}' without a compatible DTO materializer. Use a one-dimensional array or a supported list or set collection.",
        "Mississippi.Inlet.Projections",
        DiagnosticSeverity.Error,
        true);
}
