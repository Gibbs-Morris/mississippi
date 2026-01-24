using System;

using Microsoft.CodeAnalysis;


namespace Mississippi.Inlet.Generators.Core.Naming;

/// <summary>
///     Resolves target namespace information from MSBuild properties available during source generation.
/// </summary>
/// <remarks>
///     <para>
///         This class provides helper methods for namespace-agnostic code generation. Instead of hardcoding
///         ".Domain" → ".Client" transforms, generators can use the actual target project's namespace as the
///         output root.
///     </para>
/// </remarks>
public static class TargetNamespaceResolver
{
    /// <summary>
    ///     The MSBuild property key for the assembly name (fallback).
    /// </summary>
    public const string AssemblyNameProperty = "build_property.AssemblyName";

    /// <summary>
    ///     The MSBuild property key for the project's root namespace.
    /// </summary>
    public const string RootNamespaceProperty = "build_property.RootNamespace";

    /// <summary>
    ///     Extracts the aggregate name from any source namespace pattern.
    /// </summary>
    /// <param name="sourceNamespace">
    ///     The source namespace (e.g., "Contoso.Domain.Aggregates.BankAccount.Commands" or
    ///     "MyApp.CoreDomainLogic.Aggregates.BankAccount.Commands").
    /// </param>
    /// <returns>The aggregate name (e.g., "BankAccount"), or null if not found.</returns>
    public static string? ExtractAggregateName(
        string sourceNamespace
    )
    {
        if (string.IsNullOrEmpty(sourceNamespace))
        {
            return null;
        }

        // Look for .Aggregates. segment
        const string aggregatesSegment = ".Aggregates.";
        const string commandsSuffix = ".Commands";
        int aggregatesIndex = sourceNamespace.IndexOf(aggregatesSegment, StringComparison.Ordinal);
        if (aggregatesIndex < 0)
        {
            return null;
        }

        int aggregateStart = aggregatesIndex + aggregatesSegment.Length;

        // Check if namespace ends with .Commands
        if (sourceNamespace.EndsWith(commandsSuffix, StringComparison.Ordinal))
        {
            int commandsIndex = sourceNamespace.LastIndexOf(commandsSuffix, StringComparison.Ordinal);
            if (commandsIndex > aggregateStart)
            {
                return sourceNamespace.Substring(aggregateStart, commandsIndex - aggregateStart);
            }
        }

        // No .Commands suffix - take everything after .Aggregates.
        return sourceNamespace.Substring(aggregateStart);
    }

    /// <summary>
    ///     Extracts the product prefix from a source namespace by removing known segments.
    /// </summary>
    /// <param name="sourceNamespace">
    ///     The source namespace (e.g., "Contoso.Domain.Aggregates.BankAccount.Commands" or
    ///     "MyApp.CoreDomainLogic.Aggregates.BankAccount.Commands").
    /// </param>
    /// <returns>
    ///     The product prefix (e.g., "Contoso" or "MyApp"), or the full namespace if no known segment is found.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method attempts to extract the product/company prefix from a namespace by looking for
    ///         common domain-related segments. It searches for the following patterns (in order):
    ///         <list type="bullet">
    ///             <item><c>.Domain.</c> - Standard domain project pattern.</item>
    ///             <item><c>.Aggregates.</c> - Direct aggregate namespace without Domain prefix.</item>
    ///             <item><c>.Projections.</c> - Direct projection namespace without Domain prefix.</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public static string ExtractProductPrefix(
        string sourceNamespace
    )
    {
        if (string.IsNullOrEmpty(sourceNamespace))
        {
            return sourceNamespace;
        }

        // Try to find .Domain. segment first (standard pattern)
        int domainIndex = sourceNamespace.IndexOf(".Domain.", StringComparison.Ordinal);
        if (domainIndex > 0)
        {
            return sourceNamespace.Substring(0, domainIndex);
        }

        // Try to find .Aggregates. segment (non-Domain pattern)
        int aggregatesIndex = sourceNamespace.IndexOf(".Aggregates.", StringComparison.Ordinal);
        if (aggregatesIndex > 0)
        {
            return sourceNamespace.Substring(0, aggregatesIndex);
        }

        // Try to find .Projections. segment (non-Domain pattern)
        int projectionsIndex = sourceNamespace.IndexOf(".Projections.", StringComparison.Ordinal);
        if (projectionsIndex > 0)
        {
            return sourceNamespace.Substring(0, projectionsIndex);
        }

        // Can't extract - return full namespace
        return sourceNamespace;
    }

    /// <summary>
    ///     Extracts the projection name from any source namespace pattern.
    /// </summary>
    /// <param name="sourceNamespace">
    ///     The source namespace (e.g., "Contoso.Domain.Projections.BankAccountBalance" or
    ///     "MyApp.CoreDomainLogic.Projections.BankAccountBalance").
    /// </param>
    /// <returns>The projection name (e.g., "BankAccountBalance"), or null if not found.</returns>
    public static string? ExtractProjectionName(
        string sourceNamespace
    )
    {
        if (string.IsNullOrEmpty(sourceNamespace))
        {
            return null;
        }

        // Look for .Projections. segment
        const string projectionsSegment = ".Projections.";
        int projectionsIndex = sourceNamespace.IndexOf(projectionsSegment, StringComparison.Ordinal);
        if (projectionsIndex < 0)
        {
            return null;
        }

        int projectionStart = projectionsIndex + projectionsSegment.Length;

        // Take everything after .Projections.
        string remaining = sourceNamespace.Substring(projectionStart);

        // Handle nested namespaces (e.g., .Projections.BankAccount.Balance → BankAccount.Balance)
        return remaining;
    }

    /// <summary>
    ///     Gets the target root namespace from the provided values.
    /// </summary>
    /// <param name="rootNamespace">The RootNamespace property value, if available.</param>
    /// <param name="assemblyName">The AssemblyName property value, if available.</param>
    /// <param name="compilation">The compilation (used as fallback).</param>
    /// <returns>The target root namespace for generated code.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="compilation" /> is null.
    /// </exception>
    public static string GetTargetRootNamespace(
        string? rootNamespace,
        string? assemblyName,
        Compilation compilation
    )
    {
        if (compilation is null)
        {
            throw new ArgumentNullException(nameof(compilation));
        }

        // Try RootNamespace first (most accurate)
        if (!string.IsNullOrWhiteSpace(rootNamespace))
        {
            return rootNamespace!;
        }

        // Try AssemblyName as fallback
        if (!string.IsNullOrWhiteSpace(assemblyName))
        {
            return assemblyName!;
        }

        // Last resort: use compilation assembly name
        return compilation.Assembly.Name;
    }
}