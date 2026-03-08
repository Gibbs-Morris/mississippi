using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using Orleans;


namespace Mississippi.DomainModeling.TestHarness.Architecture;

/// <summary>
///     Provides deterministic validation for type-level Orleans alias attributes.
/// </summary>
public static class AliasValidation
{
    private const string ReportPathEnvironmentVariable = "MISSISSIPPI_ALIAS_VALIDATION_REPORT_PATH";

    /// <summary>
    ///     Analyzes the provided assemblies and returns mismatches plus exception-rule diagnostics.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <param name="exceptionRules">The explicit allowlisted exceptions to apply.</param>
    /// <returns>The deterministic validation summary.</returns>
    public static AliasValidationSummary AnalyzeAssemblies(
        IEnumerable<Assembly> assemblies,
        IEnumerable<AliasValidationExceptionRule> exceptionRules
    )
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        ArgumentNullException.ThrowIfNull(exceptionRules);
        ImmutableArray<Assembly> targetAssemblies = assemblies.Where(static assembly => assembly is not null)
            .DistinctBy(static assembly => assembly.FullName, StringComparer.Ordinal)
            .OrderBy(static assembly => assembly.GetName().Name, StringComparer.Ordinal)
            .ToImmutableArray();
        ImmutableArray<AliasValidationExceptionRule> configuredRules = exceptionRules.ToImmutableArray();
        ImmutableArray<Type> candidateTypes = targetAssemblies.SelectMany(static assembly => GetLoadableTypes(assembly))
            .Where(static type => !IsGeneratedType(type))
            .Where(static type => type.GetCustomAttribute<AliasAttribute>(false) is not null)
            .OrderBy(static type => type.Assembly.GetName().Name, StringComparer.Ordinal)
            .ThenBy(static type => GetTypeFullName(type), StringComparer.Ordinal)
            .ToImmutableArray();
        ImmutableArray<AliasValidationExceptionRule> normalizedRules =
            configuredRules.Select(static rule => NormalizeRule(rule)).ToImmutableArray();
        ImmutableArray<string> configurationErrors = ValidateExceptionRules(candidateTypes, normalizedRules);
        ImmutableArray<AliasValidationExceptionRule> activeExceptions = candidateTypes
            .SelectMany(type => normalizedRules.Where(rule => MatchesRule(type, rule)))
            .Distinct()
            .OrderBy(static rule => rule.TypeFullName ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(static rule => rule.ExpectedAlias ?? string.Empty, StringComparer.Ordinal)
            .ToImmutableArray();
        ImmutableArray<AliasValidationResult> mismatches = candidateTypes
            .Select(static type => CreatePotentialMismatch(type))
            .Where(static mismatch => mismatch is not null)
            .Select(static mismatch => mismatch!)
            .Where(mismatch => !normalizedRules.Any(rule => MatchesRule(mismatch, rule)))
            .OrderBy(static mismatch => mismatch.AssemblyName, StringComparer.Ordinal)
            .ThenBy(static mismatch => mismatch.TypeFullName, StringComparer.Ordinal)
            .ThenBy(static mismatch => mismatch.MismatchCategory.ToString(), StringComparer.Ordinal)
            .ToImmutableArray();
        AliasValidationSummary summary = new(mismatches, configurationErrors, activeExceptions);
        TryWriteReport(summary);
        return summary;
    }

    /// <summary>
    ///     Returns the expected alias for the specified type.
    /// </summary>
    /// <param name="type">The type to evaluate.</param>
    /// <returns>The expected alias string.</returns>
    public static string GetExpectedAlias(
        Type type
    )
    {
        ArgumentNullException.ThrowIfNull(type);
        return GetTypeFullName(type);
    }

    /// <summary>
    ///     Returns <see langword="true" /> when the specified type should be treated as generated.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true" /> when the type should be excluded.</returns>
    public static bool IsGeneratedType(
        Type type
    )
    {
        ArgumentNullException.ThrowIfNull(type);
        if (type.IsDefined(typeof(CompilerGeneratedAttribute), false))
        {
            return true;
        }

        string typeName = type.Name;
        string? typeNamespace = type.Namespace;
        return (typeNamespace?.StartsWith("OrleansCodeGen", StringComparison.Ordinal) == true) ||
               typeName.StartsWith("Codec_", StringComparison.Ordinal) ||
               typeName.StartsWith("Copier_", StringComparison.Ordinal) ||
               typeName.StartsWith("Activator_", StringComparison.Ordinal) ||
               typeName.StartsWith("Proxy_", StringComparison.Ordinal) ||
               typeName.StartsWith("Invokable_", StringComparison.Ordinal) ||
               typeName.Contains("AnonymousType", StringComparison.Ordinal);
    }

    private static AliasTypeCategory ClassifyType(
        Type type
    )
    {
        string? typeNamespace = type.Namespace;
        string typeName = type.Name;
        if ((typeNamespace?.Contains(".Commands", StringComparison.Ordinal) == true) ||
            typeName.EndsWith("Command", StringComparison.Ordinal))
        {
            return AliasTypeCategory.Command;
        }

        if ((typeNamespace?.Contains(".Events", StringComparison.Ordinal) == true) ||
            typeName.EndsWith("Event", StringComparison.Ordinal))
        {
            return AliasTypeCategory.Event;
        }

        if ((typeNamespace?.Contains(".Projections", StringComparison.Ordinal) == true) ||
            typeName.EndsWith("Projection", StringComparison.Ordinal))
        {
            return AliasTypeCategory.Projection;
        }

        if ((typeNamespace?.Contains(".Aggregates", StringComparison.Ordinal) == true) ||
            typeName.EndsWith("Aggregate", StringComparison.Ordinal) ||
            typeName.EndsWith("SagaState", StringComparison.Ordinal))
        {
            return AliasTypeCategory.Aggregate;
        }

        if (type.IsInterface && typeName.EndsWith("Grain", StringComparison.Ordinal))
        {
            return AliasTypeCategory.GrainInterface;
        }

        if (!type.IsInterface && typeName.EndsWith("Grain", StringComparison.Ordinal))
        {
            return AliasTypeCategory.GrainImplementation;
        }

        return AliasTypeCategory.Contract;
    }

    private static AliasValidationResult? CreatePotentialMismatch(
        Type type
    )
    {
        AliasAttribute aliasAttribute = type.GetCustomAttribute<AliasAttribute>(false)!;
        string actualAlias = aliasAttribute.Alias;
        string expectedAlias = GetExpectedAlias(type);
        if (string.Equals(actualAlias, expectedAlias, StringComparison.Ordinal))
        {
            return null;
        }

        return new(
            type.Assembly.GetName().Name ?? type.Assembly.FullName ?? "UnknownAssembly",
            GetTypeFullName(type),
            ClassifyType(type),
            actualAlias,
            expectedAlias,
            AliasMismatchCategory.AliasDoesNotMatchCurrentTypeName);
    }

    private static ImmutableArray<Type> GetLoadableTypes(
        Assembly assembly
    )
    {
        try
        {
            return assembly.GetTypes().ToImmutableArray();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>().ToImmutableArray();
        }
    }

    private static string GetTypeFullName(
        Type type
    ) =>
        type.FullName ?? type.Name;

    private static bool MatchesRule(
        Type type,
        AliasValidationExceptionRule rule
    )
    {
        string typeFullName = GetTypeFullName(type);
        string expectedAlias = GetExpectedAlias(type);
        return string.Equals(rule.TypeFullName, typeFullName, StringComparison.Ordinal) ||
               string.Equals(rule.ExpectedAlias, expectedAlias, StringComparison.Ordinal);
    }

    private static bool MatchesRule(
        AliasValidationResult mismatch,
        AliasValidationExceptionRule rule
    ) =>
        string.Equals(rule.TypeFullName, mismatch.TypeFullName, StringComparison.Ordinal) ||
        string.Equals(rule.ExpectedAlias, mismatch.ExpectedAlias, StringComparison.Ordinal);

    private static AliasValidationExceptionRule NormalizeRule(
        AliasValidationExceptionRule rule
    ) =>
        new(
            NormalizeValue(rule.TypeFullName),
            NormalizeValue(rule.ExpectedAlias),
            rule.Classification,
            rule.Reason.Trim(),
            NormalizeValue(rule.Owner));

    private static string? NormalizeValue(
        string? value
    ) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void TryWriteReport(
        AliasValidationSummary summary
    )
    {
        string? explicitPath = Environment.GetEnvironmentVariable(ReportPathEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(explicitPath) ||
            (summary.Mismatches.IsDefaultOrEmpty && summary.ConfigurationErrors.IsDefaultOrEmpty))
        {
            return;
        }

        string fullPath = Path.GetFullPath(explicitPath);
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(fullPath, summary.FormatReport());
    }

    private static ImmutableArray<string> ValidateExceptionRules(
        ImmutableArray<Type> candidateTypes,
        ImmutableArray<AliasValidationExceptionRule> configuredRules
    )
    {
        List<string> errors = [];
        HashSet<string> seenKeys = new(StringComparer.Ordinal);
        foreach (AliasValidationExceptionRule configuredRule in configuredRules)
        {
            string? normalizedTypeFullName = configuredRule.TypeFullName;
            string? normalizedExpectedAlias = configuredRule.ExpectedAlias;
            string normalizedReason = configuredRule.Reason;
            if (string.IsNullOrWhiteSpace(normalizedTypeFullName) && string.IsNullOrWhiteSpace(normalizedExpectedAlias))
            {
                errors.Add("Alias exception rules must specify either TypeFullName or ExpectedAlias.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(normalizedReason))
            {
                errors.Add(
                    $"Alias exception rule '{normalizedTypeFullName ?? normalizedExpectedAlias}' must include a reason.");
            }

            if ((normalizedTypeFullName?.Contains('*', StringComparison.Ordinal) == true) ||
                (normalizedExpectedAlias?.Contains('*', StringComparison.Ordinal) == true))
            {
                errors.Add(
                    $"Alias exception rule '{normalizedTypeFullName ?? normalizedExpectedAlias}' must not use wildcard matching.");
            }

            string dedupeKey = string.Concat(
                normalizedTypeFullName ?? string.Empty,
                "|",
                normalizedExpectedAlias ?? string.Empty,
                "|",
                configuredRule.Classification.ToString());
            if (!seenKeys.Add(dedupeKey))
            {
                errors.Add(
                    $"Alias exception rule '{normalizedTypeFullName ?? normalizedExpectedAlias}' is duplicated.");
            }

            bool matchedCandidate = candidateTypes.Any(type =>
                string.Equals(normalizedTypeFullName, GetTypeFullName(type), StringComparison.Ordinal) ||
                string.Equals(normalizedExpectedAlias, GetExpectedAlias(type), StringComparison.Ordinal));
            if (!matchedCandidate)
            {
                errors.Add(
                    $"Alias exception rule '{normalizedTypeFullName ?? normalizedExpectedAlias}' is stale and no longer matches a scanned type.");
            }
        }

        return errors.OrderBy(static error => error, StringComparer.Ordinal).ToImmutableArray();
    }
}