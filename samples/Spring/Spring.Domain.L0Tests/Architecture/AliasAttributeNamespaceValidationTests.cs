using Mississippi.DomainModeling.TestHarness.Architecture;

using MississippiSamples.Spring.Domain.Aggregates.BankAccount;


namespace MississippiSamples.Spring.Domain.L0Tests.Architecture;

/// <summary>
///     Verifies that Spring domain type aliases match current CLR namespaces.
/// </summary>
public sealed class AliasAttributeNamespaceValidationTests
{
    private static readonly AliasValidationSummary Summary = AliasValidation.AnalyzeAssemblies(
        [typeof(BankAccountAggregate).Assembly],
        AliasValidationExceptionRegistry.Rules);

    /// <summary>
    ///     Verifies that the centralized exception registry is valid for Spring domain types.
    /// </summary>
    [Fact]
    public void AliasExceptionRegistryShouldBeValid()
    {
        Assert.True(Summary.ConfigurationErrors.IsEmpty, Summary.FormatReport());
    }

    /// <summary>
    ///     Verifies that Spring domain aliases match their current type names.
    /// </summary>
    [Fact]
    public void SpringDomainAliasesShouldMatchCurrentTypeNames()
    {
        Assert.True(Summary.Mismatches.IsEmpty, Summary.FormatReport());
    }
}