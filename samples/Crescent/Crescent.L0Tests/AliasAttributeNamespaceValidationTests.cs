using Mississippi.DomainModeling.TestHarness.Architecture;

using MississippiSamples.Crescent.L2Tests;


namespace MississippiSamples.Crescent.L0Tests;

/// <summary>
///     Verifies that Crescent sample aliases match current CLR namespaces.
/// </summary>
public sealed class AliasAttributeNamespaceValidationTests
{
    private static readonly AliasValidationSummary Summary = AliasValidation.AnalyzeAssemblies(
        [typeof(CrescentFixture).Assembly],
        AliasValidationExceptionRegistry.Rules);

    /// <summary>
    ///     Verifies that the centralized exception registry is valid for Crescent sample types.
    /// </summary>
    [Fact]
    public void AliasExceptionRegistryShouldBeValid()
    {
        Assert.True(Summary.ConfigurationErrors.IsEmpty, Summary.FormatReport());
    }

    /// <summary>
    ///     Verifies that Crescent sample aliases match their current type names.
    /// </summary>
    [Fact]
    public void CrescentAliasesShouldMatchCurrentTypeNames()
    {
        Assert.True(Summary.Mismatches.IsEmpty, Summary.FormatReport());
    }
}