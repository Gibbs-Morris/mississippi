using System;
using System.Text;


namespace Mississippi.Sdk.Generators.Core.Naming;

/// <summary>
///     Provides naming convention utilities for source generators.
/// </summary>
public static class NamingConventions
{
    private const string AggregatesSegment = ".Aggregates.";

    private const string ClientSegment = ".Client.";

    private const string CommandsSuffix = ".Commands";

    private const string DomainAggregatesSegment = ".Domain.Aggregates.";

    private const string DomainSegment = ".Domain.";

    private const string DomainSuffix = ".Domain";

    /// <summary>
    ///     Extracts the aggregate name from a domain command namespace.
    /// </summary>
    /// <param name="domainNamespace">
    ///     The domain namespace (e.g., "Spring.Domain.Aggregates.BankAccount.Commands").
    /// </param>
    /// <returns>The aggregate name (e.g., "BankAccount"), or null if not in expected format.</returns>
    public static string? GetAggregateNameFromNamespace(
        string domainNamespace
    )
    {
        if (string.IsNullOrEmpty(domainNamespace))
        {
            return null;
        }

        // Pattern: Spring.Domain.Aggregates.BankAccount.Commands → BankAccount
        if (domainNamespace.Contains(DomainAggregatesSegment) &&
            domainNamespace.EndsWith(CommandsSuffix, StringComparison.Ordinal))
        {
            int aggregatesIndex = domainNamespace.IndexOf(AggregatesSegment, StringComparison.Ordinal);
            int commandsIndex = domainNamespace.LastIndexOf(CommandsSuffix, StringComparison.Ordinal);
            if ((aggregatesIndex > 0) && (commandsIndex > aggregatesIndex))
            {
                int aggregateStart = aggregatesIndex + AggregatesSegment.Length;
                return domainNamespace.Substring(aggregateStart, commandsIndex - aggregateStart);
            }
        }

        return null;
    }

    /// <summary>
    ///     Converts a domain command namespace to a client Actions namespace.
    /// </summary>
    /// <param name="domainNamespace">
    ///     The domain namespace (e.g., "Spring.Domain.Aggregates.BankAccount.Commands").
    /// </param>
    /// <returns>The client namespace (e.g., "Spring.Client.Features.BankAccountAggregate.Actions").</returns>
    public static string GetClientActionsNamespace(
        string domainNamespace
    ) =>
        GetClientFeatureNamespace(domainNamespace, "Actions");

    /// <summary>
    ///     Converts a domain command namespace to a client DTO namespace.
    /// </summary>
    /// <param name="domainNamespace">
    ///     The domain namespace (e.g., "Spring.Domain.Aggregates.BankAccount.Commands").
    /// </param>
    /// <returns>The client namespace (e.g., "Spring.Client.Features.BankAccountAggregate.Dtos").</returns>
    /// <remarks>
    ///     <para>
    ///         Replaces ".Domain.Aggregates.{Aggregate}.Commands" with ".Client.Features.{Aggregate}Aggregate.Dtos".
    ///         Falls back to simple ".Domain" → ".Client" replacement if pattern doesn't match.
    ///     </para>
    /// </remarks>
    public static string GetClientCommandDtoNamespace(
        string domainNamespace
    )
    {
        if (string.IsNullOrEmpty(domainNamespace))
        {
            return domainNamespace;
        }

        // Pattern: Spring.Domain.Aggregates.BankAccount.Commands → Spring.Client.Features.BankAccountAggregate.Dtos
        if (domainNamespace.Contains(DomainAggregatesSegment) &&
            domainNamespace.EndsWith(CommandsSuffix, StringComparison.Ordinal))
        {
            // Extract product prefix (everything before .Domain)
            int domainIndex = domainNamespace.IndexOf(DomainSegment, StringComparison.Ordinal);
            if (domainIndex > 0)
            {
                string product = domainNamespace.Substring(0, domainIndex);

                // Extract aggregate name from after .Aggregates. and before .Commands
                int aggregatesIndex = domainNamespace.IndexOf(AggregatesSegment, StringComparison.Ordinal);
                int commandsIndex = domainNamespace.LastIndexOf(CommandsSuffix, StringComparison.Ordinal);
                if ((aggregatesIndex > 0) && (commandsIndex > aggregatesIndex))
                {
                    int aggregateStart = aggregatesIndex + AggregatesSegment.Length;
                    string aggregateName = domainNamespace.Substring(aggregateStart, commandsIndex - aggregateStart);
                    return $"{product}.Client.Features.{aggregateName}Aggregate.Dtos";
                }
            }
        }

        // Fallback: Replace .Domain with .Client and add .Dtos suffix
        if (domainNamespace.EndsWith(CommandsSuffix, StringComparison.Ordinal))
        {
            string withoutCommands = domainNamespace.Substring(0, domainNamespace.Length - CommandsSuffix.Length);
            if (withoutCommands.Contains(DomainSegment))
            {
                return withoutCommands.Replace(DomainSegment, ClientSegment) + ".Dtos";
            }
        }

        // Last resort: just append .Client.Dtos
        return domainNamespace + ".Client.Dtos";
    }

    /// <summary>
    ///     Converts a domain command namespace to a client Effects namespace.
    /// </summary>
    /// <param name="domainNamespace">
    ///     The domain namespace (e.g., "Spring.Domain.Aggregates.BankAccount.Commands").
    /// </param>
    /// <returns>The client namespace (e.g., "Spring.Client.Features.BankAccountAggregate.Effects").</returns>
    public static string GetClientEffectsNamespace(
        string domainNamespace
    ) =>
        GetClientFeatureNamespace(domainNamespace, "Effects");

    /// <summary>
    ///     Converts a domain command namespace to the client feature root namespace (without sub-namespace).
    /// </summary>
    /// <param name="domainNamespace">
    ///     The domain namespace (e.g., "Spring.Domain.Aggregates.BankAccount.Commands").
    /// </param>
    /// <returns>The client namespace (e.g., "Spring.Client.Features.BankAccountAggregate").</returns>
    public static string GetClientFeatureRootNamespace(
        string domainNamespace
    )
    {
        string withSubNamespace = GetClientFeatureNamespace(domainNamespace, "Placeholder");

        // Remove the ".Placeholder" suffix
        return withSubNamespace.EndsWith(".Placeholder", StringComparison.Ordinal)
            ? withSubNamespace.Substring(0, withSubNamespace.Length - ".Placeholder".Length)
            : withSubNamespace;
    }

    /// <summary>
    ///     Converts a domain command namespace to a client Mappers namespace.
    /// </summary>
    /// <param name="domainNamespace">
    ///     The domain namespace (e.g., "Spring.Domain.Aggregates.BankAccount.Commands").
    /// </param>
    /// <returns>The client namespace (e.g., "Spring.Client.Features.BankAccountAggregate.Mappers").</returns>
    public static string GetClientMappersNamespace(
        string domainNamespace
    ) =>
        GetClientFeatureNamespace(domainNamespace, "Mappers");

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
        if (domainNamespace.EndsWith(DomainSuffix, StringComparison.Ordinal))
        {
            return domainNamespace.Substring(0, domainNamespace.Length - DomainSuffix.Length) + ".Client";
        }

        if (domainNamespace.Contains(DomainSegment))
        {
            return domainNamespace.Replace(DomainSegment, ClientSegment);
        }

        // Last resort: just append .Client
        return domainNamespace + ".Client";
    }

    /// <summary>
    ///     Converts a domain command namespace to a client Reducers namespace.
    /// </summary>
    /// <param name="domainNamespace">
    ///     The domain namespace (e.g., "Spring.Domain.Aggregates.BankAccount.Commands").
    /// </param>
    /// <returns>The client namespace (e.g., "Spring.Client.Features.BankAccountAggregate.Reducers").</returns>
    public static string GetClientReducersNamespace(
        string domainNamespace
    ) =>
        GetClientFeatureNamespace(domainNamespace, "Reducers");

    /// <summary>
    ///     Converts a domain command namespace to a client State namespace.
    /// </summary>
    /// <param name="domainNamespace">
    ///     The domain namespace (e.g., "Spring.Domain.Aggregates.BankAccount.Commands").
    /// </param>
    /// <returns>The client namespace (e.g., "Spring.Client.Features.BankAccountAggregate.State").</returns>
    public static string GetClientStateNamespace(
        string domainNamespace
    ) =>
        GetClientFeatureNamespace(domainNamespace, "State");

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
    ///     Gets the command request DTO name for client-side from a command type name.
    /// </summary>
    /// <param name="commandName">The command type name (e.g., "DepositFunds").</param>
    /// <returns>The request DTO name (e.g., "DepositFundsRequestDto").</returns>
    public static string GetCommandRequestDtoName(
        string commandName
    ) =>
        commandName + "RequestDto";

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
        if (domainNamespace.Contains(DomainAggregatesSegment) &&
            domainNamespace.EndsWith(CommandsSuffix, StringComparison.Ordinal))
        {
            // Extract product prefix (everything before .Domain)
            int domainIndex = domainNamespace.IndexOf(DomainSegment, StringComparison.Ordinal);
            if (domainIndex > 0)
            {
                string product = domainNamespace.Substring(0, domainIndex);
                return product + ".Server.Controllers.Aggregates";
            }
        }

        // Fallback: Replace .Domain with .Server
        if (domainNamespace.EndsWith(DomainSuffix, StringComparison.Ordinal))
        {
            return domainNamespace.Substring(0, domainNamespace.Length - DomainSuffix.Length) + ".Server";
        }

        if (domainNamespace.Contains(DomainSegment))
        {
            return domainNamespace.Replace(DomainSegment, ".Server.");
        }

        // Last resort: just append .Server
        return domainNamespace + ".Server";
    }

    /// <summary>
    ///     Converts a domain aggregate namespace to a silo registration namespace.
    /// </summary>
    /// <param name="domainNamespace">
    ///     The domain namespace (e.g., "Spring.Domain.Aggregates.BankAccount").
    /// </param>
    /// <returns>The silo namespace (e.g., "Spring.Silo.Registrations").</returns>
    /// <remarks>
    ///     <para>
    ///         Replaces ".Domain.Aggregates.{Aggregate}" with ".Silo.Registrations".
    ///         Falls back to simple ".Domain" → ".Silo" replacement if pattern doesn't match.
    ///     </para>
    /// </remarks>
    public static string GetSiloRegistrationNamespace(
        string domainNamespace
    )
    {
        if (string.IsNullOrEmpty(domainNamespace))
        {
            return domainNamespace;
        }

        // Pattern: Spring.Domain.Aggregates.BankAccount → Spring.Silo.Registrations
        // Pattern: Spring.Domain.Projections.BankAccountBalance → Spring.Silo.Registrations
        int domainIndex = domainNamespace.IndexOf(DomainSegment, StringComparison.Ordinal);
        if (domainIndex > 0)
        {
            string product = domainNamespace.Substring(0, domainIndex);
            return product + ".Silo.Registrations";
        }

        // Fallback: Replace .Domain with .Silo and add .Registrations
        if (domainNamespace.EndsWith(DomainSuffix, StringComparison.Ordinal))
        {
            return domainNamespace.Substring(0, domainNamespace.Length - DomainSuffix.Length) + ".Silo.Registrations";
        }

        if (domainNamespace.Contains(DomainSegment))
        {
            return domainNamespace.Replace(DomainSegment, ".Silo.") + ".Registrations";
        }

        // Last resort: just append .Silo.Registrations
        return domainNamespace + ".Silo.Registrations";
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

    /// <summary>
    ///     Converts a domain command namespace to a client feature sub-namespace.
    /// </summary>
    /// <param name="domainNamespace">The domain namespace.</param>
    /// <param name="subNamespace">The sub-namespace (e.g., "Actions", "Mappers", "Dtos").</param>
    /// <returns>The client feature namespace.</returns>
    private static string GetClientFeatureNamespace(
        string domainNamespace,
        string subNamespace
    )
    {
        if (string.IsNullOrEmpty(domainNamespace))
        {
            return domainNamespace;
        }

        // Pattern: Spring.Domain.Aggregates.BankAccount.Commands → Spring.Client.Features.BankAccountAggregate.{subNamespace}
        if (domainNamespace.Contains(DomainAggregatesSegment) &&
            domainNamespace.EndsWith(CommandsSuffix, StringComparison.Ordinal))
        {
            int domainIndex = domainNamespace.IndexOf(DomainSegment, StringComparison.Ordinal);
            if (domainIndex > 0)
            {
                string product = domainNamespace.Substring(0, domainIndex);
                int aggregatesIndex = domainNamespace.IndexOf(AggregatesSegment, StringComparison.Ordinal);
                int commandsIndex = domainNamespace.LastIndexOf(CommandsSuffix, StringComparison.Ordinal);
                if ((aggregatesIndex > 0) && (commandsIndex > aggregatesIndex))
                {
                    int aggregateStart = aggregatesIndex + AggregatesSegment.Length;
                    string aggregateName = domainNamespace.Substring(aggregateStart, commandsIndex - aggregateStart);
                    return $"{product}.Client.Features.{aggregateName}Aggregate.{subNamespace}";
                }
            }
        }

        // Fallback
        if (domainNamespace.EndsWith(CommandsSuffix, StringComparison.Ordinal))
        {
            string withoutCommands = domainNamespace.Substring(0, domainNamespace.Length - CommandsSuffix.Length);
            if (withoutCommands.Contains(DomainSegment))
            {
                return withoutCommands.Replace(DomainSegment, ClientSegment) + "." + subNamespace;
            }
        }

        return domainNamespace + ".Client." + subNamespace;
    }
}