using System;
using System.Linq;

using Mississippi.DomainModeling.TestHarness.Architecture;


namespace Mississippi.DomainModeling.TestHarness.L0Tests.Architecture;

/// <summary>
///     Verifies alias validation diagnostics remain actionable for test authors.
/// </summary>
public sealed class AliasValidationSummaryTests
{
    /// <summary>
    ///     Empty scans should produce an empty report without unused section headings.
    /// </summary>
    [Fact]
    public void EmptyScansShouldHaveEmptyReports()
    {
        AliasValidationSummary summary = AliasValidation.AnalyzeAssemblies([], AliasValidationExceptionRegistry.Rules);
        Assert.Empty(summary.Mismatches);
        Assert.Empty(summary.ConfigurationErrors);
        Assert.Empty(summary.ActiveExceptions);
        Assert.Empty(summary.FormatReport());
    }

    /// <summary>
    ///     Invalid rule identifiers, missing reasons, and duplicates should produce distinct diagnostics.
    /// </summary>
    [Fact]
    public void InvalidRulesShouldReportTheirSpecificProblems()
    {
        AliasValidationExceptionRule duplicated = new(
            typeof(NamespaceMismatchFixture).FullName,
            null,
            AliasExceptionClassification.NonContractHelper,
            "Fixture.");
        AliasValidationSummary summary = AliasValidation.AnalyzeAssemblies(
            [typeof(AliasValidationSummaryTests).Assembly],
            [
                new("  ", null, AliasExceptionClassification.NonContractHelper, "No identifier."),
                new(null, typeof(GenericAliasFixture<>).FullName, AliasExceptionClassification.NonContractHelper, "  "),
                duplicated,
                duplicated,
            ]);
        Assert.Equal(3, summary.ConfigurationErrors.Length);
        Assert.Contains(
            summary.ConfigurationErrors,
            error => error.Contains("either TypeFullName or ExpectedAlias", StringComparison.Ordinal));
        Assert.Contains(
            summary.ConfigurationErrors,
            error => error.Contains("must include a reason", StringComparison.Ordinal));
        Assert.Contains(
            summary.ConfigurationErrors,
            error => error.Contains("is duplicated", StringComparison.Ordinal));
        Assert.Equal(2, summary.ActiveExceptions.Length);
        Assert.Equal(
            summary.ConfigurationErrors.OrderBy(value => value, StringComparer.Ordinal),
            summary.ConfigurationErrors);
    }

    /// <summary>
    ///     Reports should include configuration errors, unresolved mismatches, and documented exceptions.
    /// </summary>
    [Fact]
    public void ReportsShouldExplainMismatchesAndActiveExceptions()
    {
        AliasValidationSummary summary = AliasValidation.AnalyzeAssemblies(
            [typeof(AliasValidationSummaryTests).Assembly],
            [
                new(
                    null,
                    typeof(NamespaceMismatchFixture).FullName,
                    AliasExceptionClassification.NonContractHelper,
                    "  Intentional test fixture.  ",
                    "  Test owner  "),
                new("Missing.Type", null, AliasExceptionClassification.NonContractHelper, "Stale rule."),
            ]);
        string report = summary.FormatReport();
        Assert.Contains("Configuration Errors:", report, StringComparison.Ordinal);
        Assert.Contains("Missing.Type", report, StringComparison.Ordinal);
        Assert.Contains("Alias Mismatches:", report, StringComparison.Ordinal);
        Assert.Contains("Wrong.GenericAliasFixture", report, StringComparison.Ordinal);
        Assert.Contains("Active Exceptions:", report, StringComparison.Ordinal);
        Assert.Contains("Intentional test fixture.", report, StringComparison.Ordinal);
        AliasValidationExceptionRule active = Assert.Single(summary.ActiveExceptions);
        Assert.Equal("Test owner", active.Owner);
        Assert.Equal("Intentional test fixture.", active.Reason);
        Assert.DoesNotContain(
            summary.Mismatches,
            mismatch => mismatch.TypeFullName == typeof(NamespaceMismatchFixture).FullName);
    }

    /// <summary>
    ///     Reports retain every diagnostic field, section boundary, and rule identity without trailing blank lines.
    /// </summary>
    [Fact]
    public void ReportsShouldHaveStableCompleteDiagnosticText()
    {
        AliasValidationSummary summary = new(
            [
                new(
                    "Example.Assembly",
                    "Examples.Current",
                    AliasTypeCategory.Contract,
                    "Legacy.Current",
                    "Examples.Current",
                    AliasMismatchCategory.AliasDoesNotMatchCurrentTypeName),
            ],
            ["Invalid rule."],
            [
                new(
                    "Examples.Current",
                    "Ignored.SecondaryIdentity",
                    AliasExceptionClassification.IntentionalPreservedIdentity,
                    "Retain published identity."),
                new(null, "Examples.AliasOnly", AliasExceptionClassification.NonContractHelper, "Helper contract."),
            ]);
        string expected = string.Join(
            Environment.NewLine,
            "Configuration Errors:",
            "- Invalid rule.",
            string.Empty,
            "Alias Mismatches:",
            "- Example.Assembly: Type Examples.Current has Alias 'Legacy.Current' but expected 'Examples.Current'; either align the Alias or add a documented exception.",
            string.Empty,
            "Active Exceptions:",
            "- IntentionalPreservedIdentity: Examples.Current (Reason: Retain published identity.)",
            "- NonContractHelper: Examples.AliasOnly (Reason: Helper contract.)");
        Assert.Equal(expected, summary.FormatReport());
    }

    /// <summary>
    ///     Missing scan inputs and types should be rejected at public entry points.
    /// </summary>
    [Fact]
    public void ValidationShouldRejectNullInputs()
    {
        Assert.Throws<ArgumentNullException>("assemblies", () => AliasValidation.AnalyzeAssemblies(null!, []));
        Assert.Throws<ArgumentNullException>("exceptionRules", () => AliasValidation.AnalyzeAssemblies([], null!));
        Assert.Throws<ArgumentNullException>("type", () => AliasValidation.GetExpectedAlias(null!));
        Assert.Throws<ArgumentNullException>("type", () => AliasValidation.IsGeneratedType(null!));
    }
}