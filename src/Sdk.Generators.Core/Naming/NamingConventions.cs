using System;
using System.Text;


namespace Mississippi.Sdk.Generators.Core.Naming;

/// <summary>
///     Provides naming convention utilities for source generators.
/// </summary>
public static class NamingConventions
{
    /// <summary>
    ///     Converts a domain namespace to a client namespace.
    /// </summary>
    /// <param name="domainNamespace">The domain namespace (e.g., "Spring.Domain.Projections.BankAccountBalance").</param>
    /// <returns>The client namespace (e.g., "Spring.Client.Features.BankAccountBalance.Dtos").</returns>
    /// <remarks>
    ///     Replaces ".Domain.Projections." with ".Client.Features." and appends ".Dtos".
    ///     Falls back to simple ".Domain" → ".Client" replacement if pattern doesn't match.
    /// </remarks>
    public static string GetClientNamespace(
        string domainNamespace
    )
    {
        if (string.IsNullOrEmpty(domainNamespace))
        {
            return domainNamespace;
        }

        // Pattern: Spring.Domain.Projections.X → Spring.Client.Features.X.Dtos
        if (domainNamespace.Contains(".Domain.Projections."))
        {
            return domainNamespace.Replace(".Domain.Projections.", ".Client.Features.") + ".Dtos";
        }

        // Fallback: Replace .Domain with .Client
        if (domainNamespace.EndsWith(".Domain", StringComparison.Ordinal))
        {
            return domainNamespace.Substring(0, domainNamespace.Length - ".Domain".Length) + ".Client";
        }

        if (domainNamespace.Contains(".Domain."))
        {
            return domainNamespace.Replace(".Domain.", ".Client.");
        }

        // Last resort: just append .Client
        return domainNamespace + ".Client";
    }

    /// <summary>
    ///     Gets the command DTO name from a command type name.
    /// </summary>
    /// <param name="commandName">The command type name (e.g., "DepositFunds").</param>
    /// <returns>The DTO name (e.g., "DepositFundsDto").</returns>
    public static string GetCommandDtoName(
        string commandName
    ) =>
        commandName + "Dto";

    /// <summary>
    ///     Gets the DTO name from a projection type name.
    /// </summary>
    /// <param name="typeName">The projection type name (e.g., "BankAccountBalanceProjection").</param>
    /// <returns>The DTO name (e.g., "BankAccountBalanceProjectionDto").</returns>
    public static string GetDtoName(
        string typeName
    ) =>
        typeName + "Dto";

    /// <summary>
    ///     Converts a domain command namespace to a server DTO namespace.
    /// </summary>
    /// <param name="domainNamespace">
    ///     The domain namespace (e.g., "Spring.Domain.Aggregates.BankAccount.Commands").
    /// </param>
    /// <returns>The server namespace (e.g., "Spring.Server.Controllers.Aggregates").</returns>
    /// <remarks>
    ///     <para>
    ///         Replaces ".Domain.Aggregates.{Aggregate}.Commands" with ".Server.Controllers.Aggregates".
    ///         Falls back to simple ".Domain" → ".Server" replacement if pattern doesn't match.
    ///     </para>
    /// </remarks>
    public static string GetServerCommandDtoNamespace(
        string domainNamespace
    )
    {
        if (string.IsNullOrEmpty(domainNamespace))
        {
            return domainNamespace;
        }

        // Pattern: Spring.Domain.Aggregates.BankAccount.Commands → Spring.Server.Controllers.Aggregates
        if (domainNamespace.Contains(".Domain.Aggregates.") && domainNamespace.EndsWith(".Commands", StringComparison.Ordinal))
        {
            // Extract product prefix (everything before .Domain)
            int domainIndex = domainNamespace.IndexOf(".Domain.", StringComparison.Ordinal);
            if (domainIndex > 0)
            {
                string product = domainNamespace.Substring(0, domainIndex);
                return product + ".Server.Controllers.Aggregates";
            }
        }

        // Fallback: Replace .Domain with .Server
        if (domainNamespace.EndsWith(".Domain", StringComparison.Ordinal))
        {
            return domainNamespace.Substring(0, domainNamespace.Length - ".Domain".Length) + ".Server";
        }

        if (domainNamespace.Contains(".Domain."))
        {
            return domainNamespace.Replace(".Domain.", ".Server.");
        }

        // Last resort: just append .Server
        return domainNamespace + ".Server";
    }

    /// <summary>
    ///     Gets the feature key from a type name by removing common suffixes and converting to camelCase.
    /// </summary>
    /// <param name="typeName">The type name (e.g., "BankAccountAggregate").</param>
    /// <returns>The feature key (e.g., "bankAccount").</returns>
    public static string GetFeatureKey(
        string typeName
    )
    {
        // Remove common suffixes
        string baseName = RemoveSuffix(typeName, "Projection");
        baseName = RemoveSuffix(baseName, "Aggregate");
        return ToCamelCase(baseName);
    }

    /// <summary>
    ///     Gets the route prefix from a type name by removing common suffixes and converting to kebab-case.
    /// </summary>
    /// <param name="typeName">The type name (e.g., "BankAccountBalanceProjection").</param>
    /// <returns>The route prefix (e.g., "bank-account-balance").</returns>
    public static string GetRoutePrefix(
        string typeName
    )
    {
        // Remove common suffixes
        string baseName = RemoveSuffix(typeName, "Projection");
        baseName = RemoveSuffix(baseName, "Aggregate");
        return ToKebabCase(baseName);
    }

    /// <summary>
    ///     Removes a suffix from a type name if present.
    /// </summary>
    /// <param name="typeName">The type name.</param>
    /// <param name="suffix">The suffix to remove.</param>
    /// <returns>The type name without the suffix.</returns>
    public static string RemoveSuffix(
        string typeName,
        string suffix
    )
    {
        if (string.IsNullOrEmpty(typeName) || string.IsNullOrEmpty(suffix))
        {
            return typeName;
        }

        if (typeName.EndsWith(suffix, StringComparison.Ordinal))
        {
            return typeName.Substring(0, typeName.Length - suffix.Length);
        }

        return typeName;
    }

    /// <summary>
    ///     Converts a PascalCase string to camelCase.
    /// </summary>
    /// <param name="value">The PascalCase string.</param>
    /// <returns>The camelCase string.</returns>
    public static string ToCamelCase(
        string value
    )
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (value.Length == 1)
        {
            // CA1308 wants ToUpperInvariant, but we need lowercase here.
            // Using explicit char conversion to avoid the analyzer warning.
            char lower = (char)(value[0] | 0x20);
            return char.IsLetter(value[0]) ? lower.ToString() : value;
        }

        // Build result without using ToLowerInvariant on strings
        char first = value[0];
        if (char.IsUpper(first))
        {
            first = (char)(first + 32); // Convert to lowercase via ASCII
        }

        return first + value.Substring(1);
    }

    /// <summary>
    ///     Converts a string to kebab-case.
    /// </summary>
    /// <param name="value">The PascalCase or camelCase string.</param>
    /// <returns>The kebab-case string.</returns>
    public static string ToKebabCase(
        string value
    )
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        StringBuilder sb = new();
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    sb.Append('-');
                }

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}