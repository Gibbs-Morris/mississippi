using Mississippi.DomainModeling.TestHarness.Architecture;


namespace Mississippi.DomainModeling.TestHarness.L0Tests.Architecture;

/// <summary>
///     Verifies alias exception diagnostics identify the rule an author must fix.
/// </summary>
public sealed class AliasExceptionRuleTests
{
    /// <summary>
    ///     Different type identities and different alias identities remain independent exception rules.
    /// </summary>
    [Fact]
    public void DifferentRuleIdentitiesShouldNotBeMarkedAsDuplicates()
    {
        AliasValidationExceptionRule typeGeneric = new(
            typeof(GenericAliasFixture<>).FullName,
            null,
            AliasExceptionClassification.NonContractHelper,
            "Generic fixture.");
        AliasValidationExceptionRule typeNamespace = new(
            typeof(NamespaceMismatchFixture).FullName,
            null,
            AliasExceptionClassification.NonContractHelper,
            "Namespace fixture.");
        AliasValidationExceptionRule aliasGeneric = typeGeneric with
        {
            TypeFullName = null,
            ExpectedAlias = typeGeneric.TypeFullName,
        };
        AliasValidationExceptionRule aliasNamespace = typeNamespace with
        {
            TypeFullName = null,
            ExpectedAlias = typeNamespace.TypeFullName,
        };
        AliasValidationSummary summary = AliasValidation.AnalyzeAssemblies(
            [typeof(AliasExceptionRuleTests).Assembly],
            [typeNamespace, aliasNamespace, typeGeneric, aliasGeneric]);
        Assert.Empty(summary.ConfigurationErrors);
        Assert.Collection(
            summary.ActiveExceptions,
            rule => Assert.Equal(aliasGeneric, rule),
            rule => Assert.Equal(aliasNamespace, rule),
            rule => Assert.Equal(typeGeneric, rule),
            rule => Assert.Equal(typeNamespace, rule));
    }

    /// <summary>
    ///     The primary type identity takes precedence when stale or duplicate rules provide both identifiers.
    /// </summary>
    /// <param name="typeFullName">The configured type identity.</param>
    /// <param name="expectedAlias">The configured expected alias.</param>
    /// <param name="diagnosticIdentity">The identifier that diagnostics should present.</param>
    [Theory]
    [InlineData("Missing.Type", null, "Missing.Type")]
    [InlineData(null, "Missing.Alias", "Missing.Alias")]
    [InlineData("Missing.Type", "Missing.Alias", "Missing.Type")]
    public void InvalidRulesShouldIdentifyTheirPrimaryIdentity(
        string? typeFullName,
        string? expectedAlias,
        string diagnosticIdentity
    )
    {
        AliasValidationExceptionRule rule = new(
            typeFullName,
            expectedAlias,
            AliasExceptionClassification.NonContractHelper,
            " ");
        AliasValidationSummary summary = AliasValidation.AnalyzeAssemblies([], [rule, rule]);
        Assert.Contains(
            $"Alias exception rule '{diagnosticIdentity}' must include a reason.",
            summary.ConfigurationErrors);
        Assert.Contains(
            $"Alias exception rule '{diagnosticIdentity}' is stale and no longer matches a scanned type.",
            summary.ConfigurationErrors);
        Assert.Contains($"Alias exception rule '{diagnosticIdentity}' is duplicated.", summary.ConfigurationErrors);
    }

    /// <summary>
    ///     Wildcards in either identifier are rejected and identify the primary rule identity.
    /// </summary>
    /// <param name="typeFullName">The configured type identity.</param>
    /// <param name="expectedAlias">The configured expected alias.</param>
    /// <param name="diagnosticIdentity">The identifier that diagnostics should present.</param>
    [Theory]
    [InlineData("Missing.*", null, "Missing.*")]
    [InlineData(null, "Missing.*", "Missing.*")]
    [InlineData("Missing.Type", "Other.*", "Missing.Type")]
    public void WildcardRulesShouldIdentifyTheirPrimaryIdentity(
        string? typeFullName,
        string? expectedAlias,
        string diagnosticIdentity
    )
    {
        AliasValidationSummary summary = AliasValidation.AnalyzeAssemblies(
            [],
            [new(typeFullName, expectedAlias, AliasExceptionClassification.NonContractHelper, "Invalid pattern.")]);
        Assert.Contains(
            $"Alias exception rule '{diagnosticIdentity}' must not use wildcard matching.",
            summary.ConfigurationErrors);
    }
}
