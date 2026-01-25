using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;

using Mississippi.Inlet.Generators.Core.Analysis;


namespace Mississippi.Inlet.Generators.Core.Diagnostics;

/// <summary>
///     Validates authorization configuration and reports diagnostics.
/// </summary>
public static class AuthorizationValidator
{
    private const string RequireSecureEndpointsAttributeFullName =
        "Mississippi.Inlet.Generators.Abstractions.RequireSecureEndpointsAttribute";

    /// <summary>
    ///     Checks if the compilation has [RequireSecureEndpoints] and returns its settings.
    /// </summary>
    /// <param name="compilation">The compilation to check.</param>
    /// <returns>The security settings, or null if the attribute is not present.</returns>
    public static SecurityEnforcementSettings? GetSecurityEnforcementSettings(
        Compilation compilation
    )
    {
        if (compilation is null)
        {
            throw new ArgumentNullException(nameof(compilation));
        }

        INamedTypeSymbol? attrSymbol = compilation.GetTypeByMetadataName(RequireSecureEndpointsAttributeFullName);
        if (attrSymbol is null)
        {
            return null;
        }

        // Check assembly-level attributes
        AttributeData? securityAttr = compilation.Assembly.GetAttributes()
            .FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attrSymbol));
        if (securityAttr is null)
        {
            return null;
        }

        bool treatAnonymousAsError = securityAttr.NamedArguments
                                         .FirstOrDefault(kvp => kvp.Key == "TreatAnonymousAsError")
                                         .Value.Value as bool? ??
                                     true;
        ImmutableArray<TypedConstant> exemptTypesArg = securityAttr.NamedArguments
            .FirstOrDefault(kvp => kvp.Key == "ExemptTypes")
            .Value.Values;
        IReadOnlyList<string> exemptTypes = exemptTypesArg.IsDefault
            ? []
            : exemptTypesArg.Select(v => v.Value?.ToString() ?? string.Empty)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        return new(treatAnonymousAsError, exemptTypes);
    }

    /// <summary>
    ///     Validates command authorization and returns any diagnostics.
    /// </summary>
    /// <param name="commandSymbol">The command type symbol.</param>
    /// <param name="aggregateTypeName">The aggregate type name for diagnostic messages.</param>
    /// <param name="commandAttr">The GenerateCommand attribute data.</param>
    /// <param name="aggregateAttr">The GenerateAggregateEndpoints attribute data (for defaults).</param>
    /// <param name="settings">The security enforcement settings, if any.</param>
    /// <returns>A list of diagnostics to report.</returns>
    public static IEnumerable<Diagnostic> ValidateCommandAuthorization(
        INamedTypeSymbol commandSymbol,
        string aggregateTypeName,
        AttributeData commandAttr,
        AttributeData aggregateAttr,
        SecurityEnforcementSettings? settings
    )
    {
        if (commandSymbol is null)
        {
            throw new ArgumentNullException(nameof(commandSymbol));
        }

        List<Diagnostic> diagnostics = new();

        // Get command-level settings (override aggregate defaults)
        string? authorizeRoles = TypeAnalyzer.GetStringProperty(commandAttr, "AuthorizeRoles");
        string? authorizePolicy = TypeAnalyzer.GetStringProperty(commandAttr, "AuthorizePolicy");
        bool? requiresAuth = TypeAnalyzer.GetNullableBooleanProperty(commandAttr, "RequiresAuthentication");
        bool allowAnonymous = TypeAnalyzer.GetBooleanProperty(commandAttr, "AllowAnonymous", false);

        // Fall back to aggregate defaults if command doesn't specify
        if (string.IsNullOrEmpty(authorizeRoles))
        {
            authorizeRoles = TypeAnalyzer.GetStringProperty(aggregateAttr, "DefaultAuthorizeRoles");
        }

        if (string.IsNullOrEmpty(authorizePolicy))
        {
            authorizePolicy = TypeAnalyzer.GetStringProperty(aggregateAttr, "DefaultAuthorizePolicy");
        }

        requiresAuth ??= TypeAnalyzer.GetNullableBooleanProperty(aggregateAttr, "DefaultRequiresAuthentication");
        bool hasAuthorization = !string.IsNullOrEmpty(authorizeRoles) ||
                                !string.IsNullOrEmpty(authorizePolicy) ||
                                (requiresAuth == true);

        // Check for conflicting settings
        if (allowAnonymous && hasAuthorization)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    GeneratorDiagnostics.ConflictingAuthorizationSettings,
                    commandSymbol.Locations.FirstOrDefault(),
                    commandSymbol.Name));
        }

        // Check security enforcement
        if (settings is not null && !settings.IsTypeExempt(commandSymbol.ToDisplayString()))
        {
            if (allowAnonymous)
            {
                if (settings.TreatAnonymousAsError)
                {
                    // AllowAnonymous with TreatAnonymousAsError = true is an error
                    diagnostics.Add(
                        Diagnostic.Create(
                            GeneratorDiagnostics.UnsecuredCommandEndpoint,
                            commandSymbol.Locations.FirstOrDefault(),
                            commandSymbol.Name,
                            aggregateTypeName));
                }
                else
                {
                    // AllowAnonymous with TreatAnonymousAsError = false is info
                    diagnostics.Add(
                        Diagnostic.Create(
                            GeneratorDiagnostics.AnonymousEndpointAllowed,
                            commandSymbol.Locations.FirstOrDefault(),
                            commandSymbol.Name));
                }
            }
            else if (!hasAuthorization)
            {
                // No authorization at all is an error
                diagnostics.Add(
                    Diagnostic.Create(
                        GeneratorDiagnostics.UnsecuredCommandEndpoint,
                        commandSymbol.Locations.FirstOrDefault(),
                        commandSymbol.Name,
                        aggregateTypeName));
            }
        }

        return diagnostics;
    }

    /// <summary>
    ///     Validates projection authorization and returns any diagnostics.
    /// </summary>
    /// <param name="projectionSymbol">The projection type symbol.</param>
    /// <param name="projectionAttr">The GenerateProjectionEndpoints attribute data.</param>
    /// <param name="settings">The security enforcement settings, if any.</param>
    /// <returns>A list of diagnostics to report.</returns>
    public static IEnumerable<Diagnostic> ValidateProjectionAuthorization(
        INamedTypeSymbol projectionSymbol,
        AttributeData projectionAttr,
        SecurityEnforcementSettings? settings
    )
    {
        if (projectionSymbol is null)
        {
            throw new ArgumentNullException(nameof(projectionSymbol));
        }

        List<Diagnostic> diagnostics = new();
        string? authorizeRoles = TypeAnalyzer.GetStringProperty(projectionAttr, "AuthorizeRoles");
        string? authorizePolicy = TypeAnalyzer.GetStringProperty(projectionAttr, "AuthorizePolicy");
        bool? requiresAuth = TypeAnalyzer.GetNullableBooleanProperty(projectionAttr, "RequiresAuthentication");
        bool allowAnonymous = TypeAnalyzer.GetBooleanProperty(projectionAttr, "AllowAnonymous", false);
        bool hasAuthorization = !string.IsNullOrEmpty(authorizeRoles) ||
                                !string.IsNullOrEmpty(authorizePolicy) ||
                                (requiresAuth == true);

        // Check for conflicting settings
        if (allowAnonymous && hasAuthorization)
        {
            diagnostics.Add(
                Diagnostic.Create(
                    GeneratorDiagnostics.ConflictingAuthorizationSettings,
                    projectionSymbol.Locations.FirstOrDefault(),
                    projectionSymbol.Name));
        }

        // Check security enforcement
        if (settings is not null && !settings.IsTypeExempt(projectionSymbol.ToDisplayString()))
        {
            if (allowAnonymous)
            {
                if (settings.TreatAnonymousAsError)
                {
                    diagnostics.Add(
                        Diagnostic.Create(
                            GeneratorDiagnostics.UnsecuredProjectionEndpoint,
                            projectionSymbol.Locations.FirstOrDefault(),
                            projectionSymbol.Name));
                }
                else
                {
                    diagnostics.Add(
                        Diagnostic.Create(
                            GeneratorDiagnostics.AnonymousEndpointAllowed,
                            projectionSymbol.Locations.FirstOrDefault(),
                            projectionSymbol.Name));
                }
            }
            else if (!hasAuthorization)
            {
                diagnostics.Add(
                    Diagnostic.Create(
                        GeneratorDiagnostics.UnsecuredProjectionEndpoint,
                        projectionSymbol.Locations.FirstOrDefault(),
                        projectionSymbol.Name));
            }
        }

        return diagnostics;
    }
}