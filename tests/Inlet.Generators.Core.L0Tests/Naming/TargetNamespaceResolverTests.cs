using System;

using Allure.Xunit.Attributes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Mississippi.Inlet.Generators.Core.Naming;


namespace Mississippi.Inlet.Generators.Core.L0Tests.Naming;

/// <summary>
///     Tests for <see cref="TargetNamespaceResolver" /> namespace resolution utilities.
/// </summary>
[AllureParentSuite("SDK")]
[AllureSuite("Generators Core")]
[AllureSubSuite("Target Namespace Resolver")]
public class TargetNamespaceResolverTests
{
    private static CSharpCompilation CreateCompilation(
        string assemblyName
    ) =>
        CSharpCompilation.Create(
            assemblyName,
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

    /// <summary>
    ///     AssemblyNameProperty should have correct value.
    /// </summary>
    [Fact]
    public void AssemblyNamePropertyHasCorrectValue()
    {
        Assert.Equal("build_property.AssemblyName", TargetNamespaceResolver.AssemblyNameProperty);
    }

    /// <summary>
    ///     ExtractAggregateName should extract aggregate from domain pattern.
    /// </summary>
    [Fact]
    public void ExtractAggregateNameExtractsFromDomainPattern()
    {
        string? result = TargetNamespaceResolver.ExtractAggregateName("Spring.Domain.Aggregates.BankAccount.Commands");
        Assert.Equal("BankAccount", result);
    }

    /// <summary>
    ///     ExtractAggregateName should extract aggregate from non-domain pattern.
    /// </summary>
    [Fact]
    public void ExtractAggregateNameExtractsFromNonDomainPattern()
    {
        string? result = TargetNamespaceResolver.ExtractAggregateName(
            "MyApp.CoreDomainLogic.Aggregates.Customer.Commands");
        Assert.Equal("Customer", result);
    }

    /// <summary>
    ///     ExtractAggregateName should extract aggregate without Commands suffix.
    /// </summary>
    [Fact]
    public void ExtractAggregateNameExtractsWithoutCommandsSuffix()
    {
        string? result = TargetNamespaceResolver.ExtractAggregateName("MyApp.Aggregates.Order");
        Assert.Equal("Order", result);
    }

    /// <summary>
    ///     ExtractAggregateName should handle nested aggregate names.
    /// </summary>
    [Fact]
    public void ExtractAggregateNameHandlesNestedNames()
    {
        string? result = TargetNamespaceResolver.ExtractAggregateName(
            "MyApp.Domain.Aggregates.Customer.Order.Commands");
        Assert.Equal("Customer.Order", result);
    }

    /// <summary>
    ///     ExtractAggregateName should return null for empty input.
    /// </summary>
    [Fact]
    public void ExtractAggregateNameReturnsNullForEmptyInput()
    {
        string? result = TargetNamespaceResolver.ExtractAggregateName(string.Empty);
        Assert.Null(result);
    }

    /// <summary>
    ///     ExtractAggregateName should return null when no Aggregates segment.
    /// </summary>
    [Fact]
    public void ExtractAggregateNameReturnsNullWhenNoAggregatesSegment()
    {
        string? result = TargetNamespaceResolver.ExtractAggregateName("MyApp.Domain.BankAccount");
        Assert.Null(result);
    }

    /// <summary>
    ///     ExtractProductPrefix should extract prefix from Aggregates pattern.
    /// </summary>
    [Fact]
    public void ExtractProductPrefixExtractsFromAggregatesPattern()
    {
        string result = TargetNamespaceResolver.ExtractProductPrefix("Contoso.Aggregates.BankAccount.Commands");
        Assert.Equal("Contoso", result);
    }

    /// <summary>
    ///     ExtractProductPrefix should extract prefix from Domain pattern.
    /// </summary>
    [Fact]
    public void ExtractProductPrefixExtractsFromDomainPattern()
    {
        string result = TargetNamespaceResolver.ExtractProductPrefix("Contoso.Domain.Aggregates.BankAccount.Commands");
        Assert.Equal("Contoso", result);
    }

    /// <summary>
    ///     ExtractProductPrefix should extract prefix from non-standard domain pattern.
    /// </summary>
    [Fact]
    public void ExtractProductPrefixExtractsFromNonStandardPattern()
    {
        string result = TargetNamespaceResolver.ExtractProductPrefix(
            "MyApp.CoreDomainLogic.Aggregates.BankAccount.Commands");
        Assert.Equal("MyApp.CoreDomainLogic", result);
    }

    /// <summary>
    ///     ExtractProductPrefix should extract prefix from Projections pattern.
    /// </summary>
    [Fact]
    public void ExtractProductPrefixExtractsFromProjectionsPattern()
    {
        string result = TargetNamespaceResolver.ExtractProductPrefix("Contoso.Projections.BankAccountBalance");
        Assert.Equal("Contoso", result);
    }

    /// <summary>
    ///     ExtractProductPrefix should return empty for empty input.
    /// </summary>
    [Fact]
    public void ExtractProductPrefixReturnsEmptyForEmptyInput()
    {
        string result = TargetNamespaceResolver.ExtractProductPrefix(string.Empty);
        Assert.Equal(string.Empty, result);
    }

    /// <summary>
    ///     ExtractProductPrefix should return full namespace when no known segment.
    /// </summary>
    [Fact]
    public void ExtractProductPrefixReturnsFullNamespaceWhenNoKnownSegment()
    {
        string result = TargetNamespaceResolver.ExtractProductPrefix("SomeRandomNamespace");
        Assert.Equal("SomeRandomNamespace", result);
    }

    /// <summary>
    ///     ExtractProjectionName should extract projection from domain pattern.
    /// </summary>
    [Fact]
    public void ExtractProjectionNameExtractsFromDomainPattern()
    {
        string? result = TargetNamespaceResolver.ExtractProjectionName("Spring.Domain.Projections.BankAccountBalance");
        Assert.Equal("BankAccountBalance", result);
    }

    /// <summary>
    ///     ExtractProjectionName should extract projection from non-domain pattern.
    /// </summary>
    [Fact]
    public void ExtractProjectionNameExtractsFromNonDomainPattern()
    {
        string? result = TargetNamespaceResolver.ExtractProjectionName(
            "MyApp.CoreDomainLogic.Projections.CustomerOrders");
        Assert.Equal("CustomerOrders", result);
    }

    /// <summary>
    ///     ExtractProjectionName should handle nested projections.
    /// </summary>
    [Fact]
    public void ExtractProjectionNameHandlesNestedProjections()
    {
        string? result = TargetNamespaceResolver.ExtractProjectionName("MyApp.Projections.BankAccount.Balance");
        Assert.Equal("BankAccount.Balance", result);
    }

    /// <summary>
    ///     ExtractProjectionName should return null for empty input.
    /// </summary>
    [Fact]
    public void ExtractProjectionNameReturnsNullForEmptyInput()
    {
        string? result = TargetNamespaceResolver.ExtractProjectionName(string.Empty);
        Assert.Null(result);
    }

    /// <summary>
    ///     ExtractProjectionName should return null when no Projections segment.
    /// </summary>
    [Fact]
    public void ExtractProjectionNameReturnsNullWhenNoProjectionsSegment()
    {
        string? result = TargetNamespaceResolver.ExtractProjectionName("MyApp.Domain.BankAccountBalance");
        Assert.Null(result);
    }

    /// <summary>
    ///     GetTargetRootNamespace should prefer RootNamespace over AssemblyName.
    /// </summary>
    [Fact]
    public void GetTargetRootNamespacePrefersRootNamespace()
    {
        Compilation compilation = CreateCompilation("TestAssembly");
        string result = TargetNamespaceResolver.GetTargetRootNamespace(
            "MyApp.Client",
            "MyApp.Client.Assembly",
            compilation);
        Assert.Equal("MyApp.Client", result);
    }

    /// <summary>
    ///     GetTargetRootNamespace should throw for null compilation.
    /// </summary>
    [Fact]
    public void GetTargetRootNamespaceThrowsForNullCompilation()
    {
        Assert.Throws<ArgumentNullException>(() => TargetNamespaceResolver.GetTargetRootNamespace("ns", "asm", null!));
    }

    /// <summary>
    ///     GetTargetRootNamespace should use AssemblyName when RootNamespace is empty.
    /// </summary>
    [Fact]
    public void GetTargetRootNamespaceUsesAssemblyNameWhenRootNamespaceEmpty()
    {
        Compilation compilation = CreateCompilation("TestAssembly");
        string result = TargetNamespaceResolver.GetTargetRootNamespace(
            string.Empty,
            "MyApp.Client.Assembly",
            compilation);
        Assert.Equal("MyApp.Client.Assembly", result);
    }

    /// <summary>
    ///     GetTargetRootNamespace should use AssemblyName when RootNamespace is null.
    /// </summary>
    [Fact]
    public void GetTargetRootNamespaceUsesAssemblyNameWhenRootNamespaceNull()
    {
        Compilation compilation = CreateCompilation("TestAssembly");
        string result = TargetNamespaceResolver.GetTargetRootNamespace(null, "MyApp.Client.Assembly", compilation);
        Assert.Equal("MyApp.Client.Assembly", result);
    }

    /// <summary>
    ///     GetTargetRootNamespace should use compilation assembly name as fallback.
    /// </summary>
    [Fact]
    public void GetTargetRootNamespaceUsesCompilationAssemblyNameAsFallback()
    {
        Compilation compilation = CreateCompilation("FallbackAssemblyName");
        string result = TargetNamespaceResolver.GetTargetRootNamespace(null, null, compilation);
        Assert.Equal("FallbackAssemblyName", result);
    }

    /// <summary>
    ///     RootNamespaceProperty should have correct value.
    /// </summary>
    [Fact]
    public void RootNamespacePropertyHasCorrectValue()
    {
        Assert.Equal("build_property.RootNamespace", TargetNamespaceResolver.RootNamespaceProperty);
    }
}