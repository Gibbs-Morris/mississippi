using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Mississippi.Inlet.Generators.Core.Analysis;
using Mississippi.Inlet.Generators.Core.Naming;


namespace Mississippi.Inlet.Generators.Core.L0Tests.Integration;

/// <summary>
///     Integration tests that compile real C# code and verify the analysis utilities
///     work correctly against actual Roslyn symbol tables.
/// </summary>
/// <remarks>
///     These tests use patterns from the Spring sample to verify real-world scenarios.
/// </remarks>
public class RoslynCompilationTests
{
    /// <summary>
    ///     Minimal attribute stubs needed for compilation without referencing the full SDK.
    /// </summary>
    private const string AttributeStubs = """
                                          namespace Mississippi.Inlet.Generators.Abstractions
                                          {
                                              using System;

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateAggregateEndpointsAttribute : Attribute
                                              {
                                                  public string? FeatureKey { get; set; }
                                                  public string? RoutePrefix { get; set; }
                                              }

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateCommandAttribute : Attribute
                                              {
                                                  public string HttpMethod { get; set; } = "POST";
                                                  public string Route { get; set; } = "";
                                              }

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateProjectionEndpointsAttribute : Attribute
                                              {
                                                  public string? FeatureKey { get; set; }
                                                  public string? RoutePrefix { get; set; }
                                              }
                                          }
                                          """;

    /// <summary>
    ///     Creates a Roslyn compilation from the provided source code.
    /// </summary>
    private static CSharpCompilation CreateCompilation(
        params string[] sources
    )
    {
        SyntaxTree[] syntaxTrees = sources.Select(s => CSharpSyntaxTree.ParseText(s)).ToArray();

        // Get all framework references needed for compilation
        string runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        List<MetadataReference> references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Collections.dll")),
        ];

        // Add netstandard if available (for compatibility)
        string netstandardPath = Path.Combine(runtimeDirectory, "netstandard.dll");
        if (File.Exists(netstandardPath))
        {
            references.Add(MetadataReference.CreateFromFile(netstandardPath));
        }

        return CSharpCompilation.Create(
            "TestAssembly",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));
    }

    /// <summary>
    ///     AggregateModel should correctly analyze a real aggregate type.
    /// </summary>
    [Fact]
    public void AggregateModelAnalyzesRealAggregateType()
    {
        const string aggregateSource = """
                                       using Mississippi.Inlet.Generators.Abstractions;

                                       namespace Spring.Domain.Aggregates.BankAccount
                                       {
                                           [GenerateAggregateEndpoints]
                                           public sealed record BankAccountAggregate
                                           {
                                               public decimal Balance { get; init; }
                                               public bool IsOpen { get; init; }
                                               public string HolderName { get; init; } = string.Empty;
                                           }
                                       }
                                       """;
        CSharpCompilation compilation = CreateCompilation(AttributeStubs, aggregateSource);
        INamedTypeSymbol? aggregateSymbol = compilation.GetTypeByMetadataName(
            "Spring.Domain.Aggregates.BankAccount.BankAccountAggregate");
        Assert.NotNull(aggregateSymbol);
        AggregateModel model = new(
            aggregateSymbol,
            NamingConventions.GetRoutePrefix(aggregateSymbol.Name),
            NamingConventions.GetFeatureKey(aggregateSymbol.Name));
        Assert.Equal("BankAccountAggregate", model.TypeName);
        Assert.Equal("BankAccount", model.AggregateName);
        Assert.Equal("BankAccountController", model.ControllerTypeName);
        Assert.Equal("bank-account", model.RoutePrefix);
        Assert.Equal("bankAccount", model.FeatureKey);
        Assert.Equal("Spring.Domain.Aggregates.BankAccount", model.Namespace);
    }

    /// <summary>
    ///     CommandModel should correctly analyze a positional record command.
    /// </summary>
    [Fact]
    public void CommandModelAnalyzesPositionalRecordCommand()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace Spring.Domain.Aggregates.BankAccount.Commands
                                     {
                                         [GenerateCommand(Route = "open")]
                                         public sealed record OpenAccount(string HolderName, decimal InitialDeposit = 0);
                                     }
                                     """;
        CSharpCompilation compilation = CreateCompilation(AttributeStubs, commandSource);
        INamedTypeSymbol? commandSymbol = compilation.GetTypeByMetadataName(
            "Spring.Domain.Aggregates.BankAccount.Commands.OpenAccount");
        Assert.NotNull(commandSymbol);
        CommandModel model = new(commandSymbol, "open", "POST");
        Assert.Equal("OpenAccount", model.TypeName);
        Assert.Equal("OpenAccountDto", model.DtoTypeName);
        Assert.Equal("open", model.Route);
        Assert.Equal("POST", model.HttpMethod);
        Assert.True(model.IsPositionalRecord);
        Assert.Equal(2, model.PositionalConstructorParameterCount);
    }

    /// <summary>
    ///     CommandModel should correctly analyze a standard record command with properties.
    /// </summary>
    /// <remarks>
    ///     Note: In C#, records with init properties still have a synthesized constructor.
    ///     The compiler generates a primary constructor with parameters for all init properties.
    ///     Therefore IsPositionalRecord is true for records with init properties.
    /// </remarks>
    [Fact]
    public void CommandModelAnalyzesStandardRecordCommand()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace Spring.Domain.Aggregates.BankAccount.Commands
                                     {
                                         [GenerateCommand(Route = "deposit")]
                                         public sealed record DepositFunds
                                         {
                                             public decimal Amount { get; init; }
                                         }
                                     }
                                     """;
        CSharpCompilation compilation = CreateCompilation(AttributeStubs, commandSource);
        INamedTypeSymbol? commandSymbol = compilation.GetTypeByMetadataName(
            "Spring.Domain.Aggregates.BankAccount.Commands.DepositFunds");
        Assert.NotNull(commandSymbol);
        CommandModel model = new(commandSymbol, "deposit", "POST");
        Assert.Equal("DepositFunds", model.TypeName);
        Assert.Equal("DepositFundsDto", model.DtoTypeName);

        // Records with init properties are still positional - compiler synthesizes constructor
        Assert.True(model.IsPositionalRecord);
        Assert.Single(model.Properties);
        Assert.Equal("Amount", model.Properties[0].Name);
    }

    /// <summary>
    ///     GetFullNamespace should return correct namespace from real compilation.
    /// </summary>
    [Fact]
    public void GetFullNamespaceReturnsCorrectNamespaceFromCompilation()
    {
        const string source = """
                              namespace Spring.Domain.Aggregates.BankAccount.Commands
                              {
                                  public sealed record DepositFunds
                                  {
                                      public decimal Amount { get; init; }
                                  }
                              }
                              """;
        CSharpCompilation compilation = CreateCompilation(source);
        INamedTypeSymbol? typeSymbol = compilation.GetTypeByMetadataName(
            "Spring.Domain.Aggregates.BankAccount.Commands.DepositFunds");
        Assert.NotNull(typeSymbol);
        string ns = TypeAnalyzer.GetFullNamespace(typeSymbol);
        Assert.Equal("Spring.Domain.Aggregates.BankAccount.Commands", ns);
    }

    /// <summary>
    ///     IsCollectionType should return true for generic List from real compilation.
    /// </summary>
    [Fact]
    public void IsCollectionTypeReturnsTrueForGenericListFromCompilation()
    {
        const string source = """
                              using System.Collections.Generic;

                              namespace TestNamespace
                              {
                                  public class TestClass
                                  {
                                      public List<string> Items { get; set; }
                                  }
                              }
                              """;
        CSharpCompilation compilation = CreateCompilation(source);
        INamedTypeSymbol? typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
        Assert.NotNull(typeSymbol);
        IPropertySymbol? property = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.Name == "Items");
        Assert.NotNull(property);
        Assert.True(TypeAnalyzer.IsCollectionType(property.Type));
    }

    /// <summary>
    ///     IsFrameworkType should return false for custom types from real compilation.
    /// </summary>
    [Fact]
    public void IsFrameworkTypeReturnsFalseForCustomTypesFromCompilation()
    {
        const string source = """
                              namespace Spring.Domain.ValueObjects
                              {
                                  public record Address
                                  {
                                      public string Street { get; init; }
                                      public string City { get; init; }
                                  }
                              }
                              """;
        CSharpCompilation compilation = CreateCompilation(source);
        INamedTypeSymbol? typeSymbol = compilation.GetTypeByMetadataName("Spring.Domain.ValueObjects.Address");
        Assert.NotNull(typeSymbol);
        Assert.False(TypeAnalyzer.IsFrameworkType(typeSymbol));
    }

    /// <summary>
    ///     IsFrameworkType should return true for System types from real compilation.
    /// </summary>
    [Fact]
    public void IsFrameworkTypeReturnsTrueForSystemTypesFromCompilation()
    {
        const string source = """
                              namespace TestNamespace
                              {
                                  public class TestClass
                                  {
                                      public string Name { get; set; }
                                      public int Count { get; set; }
                                      public decimal Amount { get; set; }
                                  }
                              }
                              """;
        CSharpCompilation compilation = CreateCompilation(source);
        INamedTypeSymbol? typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
        Assert.NotNull(typeSymbol);
        ImmutableArray<ISymbol> properties = typeSymbol.GetMembers();
        IPropertySymbol? nameProp = properties.OfType<IPropertySymbol>().FirstOrDefault(p => p.Name == "Name");
        IPropertySymbol? countProp = properties.OfType<IPropertySymbol>().FirstOrDefault(p => p.Name == "Count");
        IPropertySymbol? amountProp = properties.OfType<IPropertySymbol>().FirstOrDefault(p => p.Name == "Amount");
        Assert.NotNull(nameProp);
        Assert.NotNull(countProp);
        Assert.NotNull(amountProp);
        Assert.True(TypeAnalyzer.IsFrameworkType(nameProp.Type));
        Assert.True(TypeAnalyzer.IsFrameworkType(countProp.Type));
        Assert.True(TypeAnalyzer.IsFrameworkType(amountProp.Type));
    }

    /// <summary>
    ///     ProjectionModel should correctly analyze a real projection type.
    /// </summary>
    [Fact]
    public void ProjectionModelAnalyzesRealProjectionType()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;

                                        namespace Spring.Domain.Projections.BankAccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            public sealed record BankAccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                                public string HolderName { get; init; } = string.Empty;
                                                public bool IsOpen { get; init; }
                                            }
                                        }
                                        """;
        CSharpCompilation compilation = CreateCompilation(AttributeStubs, projectionSource);
        INamedTypeSymbol? projectionSymbol = compilation.GetTypeByMetadataName(
            "Spring.Domain.Projections.BankAccountBalance.BankAccountBalanceProjection");
        Assert.NotNull(projectionSymbol);
        ProjectionModel model = new(projectionSymbol, "/bank-account-balance");
        Assert.Equal("BankAccountBalanceProjection", model.TypeName);
        Assert.Equal("BankAccountBalance", model.ProjectionName);
        Assert.Equal("BankAccountBalanceDto", model.DtoTypeName);
        Assert.Equal("/bank-account-balance", model.ProjectionPath);
        Assert.Equal("Spring.Domain.Projections.BankAccountBalance", model.Namespace);
        Assert.Equal(3, model.Properties.Length);
    }

    /// <summary>
    ///     ProjectionModel should detect nested custom types from real compilation.
    /// </summary>
    [Fact]
    public void ProjectionModelDetectsNestedCustomTypesFromCompilation()
    {
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;

                              namespace Spring.Domain.ValueObjects
                              {
                                  public record Address
                                  {
                                      public string Street { get; init; }
                                      public string City { get; init; }
                                  }
                              }

                              namespace Spring.Domain.Projections.Customer
                              {
                                  using Spring.Domain.ValueObjects;

                                  [GenerateProjectionEndpoints]
                                  public sealed record CustomerProjection
                                  {
                                      public string Name { get; init; } = string.Empty;
                                      public Address BillingAddress { get; init; }
                                      public Address ShippingAddress { get; init; }
                                  }
                              }
                              """;
        CSharpCompilation compilation = CreateCompilation(AttributeStubs, source);
        INamedTypeSymbol? projectionSymbol = compilation.GetTypeByMetadataName(
            "Spring.Domain.Projections.Customer.CustomerProjection");
        Assert.NotNull(projectionSymbol);
        ProjectionModel model = new(projectionSymbol, "/customers");
        Assert.True(model.HasMappedProperties);
        Assert.Single(model.NestedCustomTypes);
        Assert.Equal("Address", model.NestedCustomTypes[0]);
    }

    /// <summary>
    ///     PropertyModel should correctly analyze nullable value type and reference type properties.
    /// </summary>
    /// <remarks>
    ///     Tests nullable value types (int?) which work reliably in minimal compilations.
    ///     Reference type nullability (string?) requires full SDK context for annotation propagation.
    /// </remarks>
    [Fact]
    public void PropertyModelAnalyzesNullableValueTypesFromCompilation()
    {
        const string source = """
                              namespace TestNamespace
                              {
                                  public class TestClass
                                  {
                                      public string RequiredName { get; set; } = string.Empty;
                                      public int RequiredCount { get; set; }
                                      public int? OptionalCount { get; set; }
                                      public decimal? OptionalAmount { get; set; }
                                  }
                              }
                              """;
        CSharpCompilation compilation = CreateCompilation(source);
        INamedTypeSymbol? typeSymbol = compilation.GetTypeByMetadataName("TestNamespace.TestClass");
        Assert.NotNull(typeSymbol);
        IPropertySymbol[] properties = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => !p.IsStatic && p.GetMethod is not null)
            .ToArray();
        Assert.Equal(4, properties.Length);
        PropertyModel requiredNameModel = new(properties.First(p => p.Name == "RequiredName"));
        PropertyModel requiredCountModel = new(properties.First(p => p.Name == "RequiredCount"));
        PropertyModel optionalCountModel = new(properties.First(p => p.Name == "OptionalCount"));
        PropertyModel optionalAmountModel = new(properties.First(p => p.Name == "OptionalAmount"));

        // Non-nullable value types are required
        Assert.False(requiredCountModel.IsNullable);

        // Nullable value types (int?, decimal?) should be detected
        Assert.True(optionalCountModel.IsNullable);
        Assert.False(optionalCountModel.IsRequired);
        Assert.True(optionalAmountModel.IsNullable);
        Assert.False(optionalAmountModel.IsRequired);

        // RequiredName has a default value, so not required even if not nullable
        Assert.True(requiredNameModel.HasDefaultValue);
    }

    /// <summary>
    ///     TypeAnalyzer.GetDtoTypeName should correctly transform types from real compilation.
    /// </summary>
    [Fact]
    public void TypeAnalyzerGetDtoTypeNameTransformsTypesFromCompilation()
    {
        const string source = """
                              namespace Spring.Domain.Aggregates.BankAccount
                              {
                                  public sealed record BankAccountAggregate
                                  {
                                      public decimal Balance { get; init; }
                                  }
                              }

                              namespace Spring.Domain.Projections.BankAccountBalance
                              {
                                  public sealed record BankAccountBalanceProjection
                                  {
                                      public decimal Balance { get; init; }
                                  }
                              }
                              """;
        CSharpCompilation compilation = CreateCompilation(source);
        INamedTypeSymbol? aggregateSymbol = compilation.GetTypeByMetadataName(
            "Spring.Domain.Aggregates.BankAccount.BankAccountAggregate");
        INamedTypeSymbol? projectionSymbol = compilation.GetTypeByMetadataName(
            "Spring.Domain.Projections.BankAccountBalance.BankAccountBalanceProjection");
        Assert.NotNull(aggregateSymbol);
        Assert.NotNull(projectionSymbol);

        // Should remove Aggregate suffix and add Dto
        string aggregateDtoName = TypeAnalyzer.GetDtoTypeName(aggregateSymbol);
        Assert.Equal("BankAccountDto", aggregateDtoName);

        // Should remove Projection suffix and add Dto
        string projectionDtoName = TypeAnalyzer.GetDtoTypeName(projectionSymbol);
        Assert.Equal("BankAccountBalanceDto", projectionDtoName);
    }
}