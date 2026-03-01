using System;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;

using Mississippi.Inlet.Generators.Core.Emit;


namespace Mississippi.Inlet.Generators.Core.Analysis;

/// <summary>
///     Shared analysis helpers for generated HTTP API authorization metadata.
/// </summary>
public static class GeneratedApiAuthorizationAnalysis
{
    private const string AllowAnonymousOnMutatingMessage =
        "[GenerateAllowAnonymous] is applied to mutating generated endpoint surface '{0}'. Review security implications.";

    /// <summary>
    ///     Diagnostic emitted when generated authorization list metadata has empty tokens.
    /// </summary>
    public static readonly DiagnosticDescriptor MalformedListMetadata = new(
        "INLETAUTH001",
        "Malformed generated authorization list metadata",
        "{0} contains empty entries. Remove empty values and keep a comma-delimited list.",
        "Inlet.Gateway.Generators",
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     Diagnostic emitted when anonymous access is requested for mutating generated endpoints.
    /// </summary>
    public static readonly DiagnosticDescriptor AllowAnonymousOnMutatingEndpoint = new(
        "INLETAUTH002",
        "AllowAnonymous on mutating generated endpoint",
        AllowAnonymousOnMutatingMessage,
        "Inlet.Gateway.Generators",
        DiagnosticSeverity.Warning,
        true);

    /// <summary>
    ///     Analyzes source-generation authorization metadata from a type.
    /// </summary>
    /// <param name="typeSymbol">The type symbol being analyzed.</param>
    /// <param name="generateAuthorizationAttribute">The resolved GenerateAuthorization attribute symbol.</param>
    /// <param name="generateAllowAnonymousAttribute">The resolved GenerateAllowAnonymous attribute symbol.</param>
    /// <param name="isMutatingEndpoint">A value indicating whether the target represents a mutating endpoint surface.</param>
    /// <returns>The resolved authorization metadata and diagnostics.</returns>
    public static GeneratedApiAuthorizationModel Analyze(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol? generateAuthorizationAttribute,
        INamedTypeSymbol? generateAllowAnonymousAttribute,
        bool isMutatingEndpoint
    )
    {
        if (typeSymbol is null)
        {
            throw new ArgumentNullException(nameof(typeSymbol));
        }

        ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        AttributeData? authorizationAttribute = typeSymbol.GetAttributes()
            .FirstOrDefault(attr =>
                generateAuthorizationAttribute is not null &&
                SymbolEqualityComparer.Default.Equals(attr.AttributeClass, generateAuthorizationAttribute));
        bool hasAllowAnonymous = typeSymbol.GetAttributes()
            .Any(attr => generateAllowAnonymousAttribute is not null &&
                         SymbolEqualityComparer.Default.Equals(attr.AttributeClass, generateAllowAnonymousAttribute));
        string? policy = null;
        string? roles = null;
        string? authenticationSchemes = null;
        if (authorizationAttribute is not null)
        {
            policy = GetNamedString(authorizationAttribute, "Policy");
            roles = NormalizeListAndEmitDiagnostic(
                authorizationAttribute,
                GetNamedString(authorizationAttribute, "Roles"),
                "Roles",
                diagnostics);
            authenticationSchemes = NormalizeListAndEmitDiagnostic(
                authorizationAttribute,
                GetNamedString(authorizationAttribute, "AuthenticationSchemes"),
                "AuthenticationSchemes",
                diagnostics);
        }

        if (hasAllowAnonymous && isMutatingEndpoint)
        {
            Location location = GetLocation(
                typeSymbol.GetAttributes()
                    .First(attr =>
                        generateAllowAnonymousAttribute is not null &&
                        SymbolEqualityComparer.Default.Equals(attr.AttributeClass, generateAllowAnonymousAttribute)));
            diagnostics.Add(Diagnostic.Create(AllowAnonymousOnMutatingEndpoint, location, typeSymbol.Name));
        }

        return new(
            policy,
            roles,
            authenticationSchemes,
            authorizationAttribute is not null,
            hasAllowAnonymous,
            diagnostics.ToImmutable());
    }

    /// <summary>
    ///     Appends authorization attributes to a source builder in deterministic order.
    /// </summary>
    /// <param name="builder">The builder receiving generated attributes.</param>
    /// <param name="authorization">Resolved authorization metadata.</param>
    public static void AppendAuthorizationAttributes(
        SourceBuilder builder,
        GeneratedApiAuthorizationModel authorization
    )
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (authorization is null)
        {
            throw new ArgumentNullException(nameof(authorization));
        }

        if (authorization.HasAllowAnonymous)
        {
            builder.AppendLine("[AllowAnonymous]");
        }

        if (!authorization.HasAuthorize)
        {
            return;
        }

        string[] parts =
        [
            BuildAuthorizePart("Policy", authorization.Policy),
            BuildAuthorizePart("Roles", authorization.Roles),
            BuildAuthorizePart("AuthenticationSchemes", authorization.AuthenticationSchemes),
        ];
        string[] populatedParts = parts.Where(part => !string.IsNullOrWhiteSpace(part)).ToArray();
        if (populatedParts.Length == 0)
        {
            builder.AppendLine("[Authorize]");
            return;
        }

        builder.AppendLine($"[Authorize({string.Join(", ", populatedParts)})]");
    }

    private static string BuildAuthorizePart(
        string propertyName,
        string? value
    ) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : propertyName + " = \"" + EscapeForCSharpStringLiteral(value!) + "\"";

    private static string EscapeForCSharpStringLiteral(
        string value
    ) =>
        value.Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");

    private static Location GetLocation(
        AttributeData attribute
    ) =>
        attribute.ApplicationSyntaxReference?.GetSyntax()?.GetLocation() ?? Location.None;

    private static string? GetNamedString(
        AttributeData attribute,
        string name
    ) =>
        attribute.NamedArguments.FirstOrDefault(kvp => kvp.Key == name).Value.Value?.ToString();

    private static string? NormalizeListAndEmitDiagnostic(
        AttributeData attribute,
        string? value,
        string fieldName,
        ImmutableArray<Diagnostic>.Builder diagnostics
    )
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string nonNullValue = value!;
        string[] tokens = nonNullValue.Split(',');
        bool hasEmptyToken = tokens.Any(token => string.IsNullOrWhiteSpace(token));
        string normalized = string.Join(
            ",",
            tokens.Where(token => !string.IsNullOrWhiteSpace(token)).Select(token => token!.Trim()));
        if (hasEmptyToken)
        {
            diagnostics.Add(Diagnostic.Create(MalformedListMetadata, GetLocation(attribute), fieldName));
        }

        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}