using System.Collections.Immutable;
using System.Text;


namespace Mississippi.DomainModeling.TestHarness.Architecture;

/// <summary>
///     Represents the full outcome of an alias validation scan.
/// </summary>
/// <param name="Mismatches">The discovered non-allowlisted mismatches.</param>
/// <param name="ConfigurationErrors">The exception-registry configuration problems.</param>
/// <param name="ActiveExceptions">The rules that matched at least one scanned type.</param>
public sealed record AliasValidationSummary(
    ImmutableArray<AliasValidationResult> Mismatches,
    ImmutableArray<string> ConfigurationErrors,
    ImmutableArray<AliasValidationExceptionRule> ActiveExceptions
)
{
    /// <summary>
    ///     Formats the current summary into a deterministic multi-line report.
    /// </summary>
    /// <returns>The formatted report text.</returns>
    public string FormatReport()
    {
        StringBuilder builder = new();
        if (!ConfigurationErrors.IsDefaultOrEmpty)
        {
            builder.AppendLine("Configuration Errors:");
            foreach (string configurationError in ConfigurationErrors)
            {
                builder.Append("- ").AppendLine(configurationError);
            }

            builder.AppendLine();
        }

        if (!Mismatches.IsDefaultOrEmpty)
        {
            builder.AppendLine("Alias Mismatches:");
            foreach (AliasValidationResult mismatch in Mismatches)
            {
                builder.Append("- ")
                    .Append(mismatch.AssemblyName)
                    .Append(": Type ")
                    .Append(mismatch.TypeFullName)
                    .Append(" has Alias '")
                    .Append(mismatch.ActualAlias)
                    .Append("' but expected '")
                    .Append(mismatch.ExpectedAlias)
                    .AppendLine("'; either align the Alias or add a documented exception.");
            }

            builder.AppendLine();
        }

        if (!ActiveExceptions.IsDefaultOrEmpty)
        {
            builder.AppendLine("Active Exceptions:");
            foreach (AliasValidationExceptionRule activeException in ActiveExceptions)
            {
                builder.Append("- ")
                    .Append(activeException.Classification)
                    .Append(": ")
                    .Append(activeException.TypeFullName ?? activeException.ExpectedAlias)
                    .Append(" (Reason: ")
                    .Append(activeException.Reason)
                    .AppendLine(")");
            }
        }

        return builder.ToString().TrimEnd();
    }
}