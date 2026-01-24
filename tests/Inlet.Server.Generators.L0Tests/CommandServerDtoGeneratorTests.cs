using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Allure.Xunit.Attributes;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Server.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="CommandServerDtoGenerator" />.
/// </summary>
[AllureParentSuite("SDK")]
[AllureSuite("Server Generators")]
[AllureSubSuite("Command Server DTO Generator")]
public class CommandServerDtoGeneratorTests
{
    /// <summary>
    ///     Minimal attribute stubs needed for compilation without referencing the full SDK.
    /// </summary>
    private const string AttributeStubs = """
                                          namespace Mississippi.Inlet.Generators.Abstractions
                                          {
                                              using System;

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateCommandAttribute : Attribute
                                              {
                                                  public string HttpMethod { get; set; } = "POST";
                                                  public string Route { get; set; } = "";
                                              }
                                          }
                                          """;

    /// <summary>
    ///     Creates a Roslyn compilation from the provided source code and runs the generator.
    /// </summary>
    private static (Compilation OutputCompilation, ImmutableArray<Diagnostic> Diagnostics, GeneratorDriverRunResult
        RunResult) RunGenerator(
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

        // Use "TestApp.Server" as assembly name - the generator will use this as the target root namespace
        // when no RootNamespace MSBuild property is available
        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestApp.Server",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));

        // Run the generator
        CommandServerDtoGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Commands from different aggregates should have separate registrations files.
    /// </summary>
    [Fact]
    public void CommandsFromDifferentAggregatesHaveSeparateRegistrationsFiles()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand(Route = "create")]
                                         public sealed record CreateOrder
                                         {
                                             public string CustomerName { get; init; } = string.Empty;
                                         }
                                     }

                                     namespace TestApp.Domain.Aggregates.Product.Commands
                                     {
                                         [GenerateCommand(Route = "create")]
                                         public sealed record CreateProduct
                                         {
                                             public string ProductName { get; init; } = string.Empty;
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);

        // 2 aggregates × (1 DTO + 1 Mapper + 1 Registration) = 6 files
        Assert.Equal(6, runResult.GeneratedTrees.Length);
        bool hasOrderRegistrations = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "OrderAggregateMapperRegistrations",
            StringComparison.Ordinal));
        bool hasProductRegistrations = runResult.GeneratedTrees.Any(t => t.FilePath.Contains(
            "ProductAggregateMapperRegistrations",
            StringComparison.Ordinal));
        Assert.True(hasOrderRegistrations);
        Assert.True(hasProductRegistrations);
    }

    /// <summary>
    ///     Generated aggregate registrations should have correct extension method name.
    /// </summary>
    [Fact]
    public void GeneratedAggregateRegistrationsHasCorrectMethodName()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand(Route = "create")]
                                         public sealed record CreateOrder
                                         {
                                             public string CustomerName { get; init; } = string.Empty;
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        string? registrationsSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("AggregateMapperRegistrations", StringComparison.Ordinal))
            ?.GetText()
            .ToString();
        Assert.NotNull(registrationsSource);
        Assert.Contains("AddOrderAggregateMappers", registrationsSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated aggregate registrations should register all command mappers.
    /// </summary>
    [Fact]
    public void GeneratedAggregateRegistrationsRegistersAllMappers()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand(Route = "create")]
                                         public sealed record CreateOrder
                                         {
                                             public string CustomerName { get; init; } = string.Empty;
                                         }

                                         [GenerateCommand(Route = "update")]
                                         public sealed record UpdateOrder
                                         {
                                             public decimal NewAmount { get; init; }
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        string? registrationsSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("AggregateMapperRegistrations", StringComparison.Ordinal))
            ?.GetText()
            .ToString();
        Assert.NotNull(registrationsSource);
        Assert.Contains(
            "AddMapper<CreateOrderDto, CreateOrder, CreateOrderDtoMapper>",
            registrationsSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "AddMapper<UpdateOrderDto, UpdateOrder, UpdateOrderDtoMapper>",
            registrationsSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated files should have correct namespace transformation.
    /// </summary>
    [Fact]
    public void GeneratedDtoHasCorrectNamespace()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand(Route = "create")]
                                         public sealed record CreateOrder
                                         {
                                             public string CustomerName { get; init; } = string.Empty;
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("CreateOrderDto", StringComparison.Ordinal))
            ?.GetText()
            .ToString();
        Assert.NotNull(dtoSource);

        // Domain.Aggregates.*.Commands → Server.Controllers.Aggregates
        Assert.Contains("namespace TestApp.Server.Controllers.Aggregates;", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should have JsonPropertyName attributes with camelCase.
    /// </summary>
    [Fact]
    public void GeneratedDtoHasJsonPropertyNameAttributes()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand(Route = "create")]
                                         public sealed record CreateOrder
                                         {
                                             public string CustomerName { get; init; } = string.Empty;
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("CreateOrderDto", StringComparison.Ordinal))
            ?.GetText()
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("[JsonPropertyName(\"customerName\")]", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should have XML documentation.
    /// </summary>
    [Fact]
    public void GeneratedDtoHasXmlDocumentation()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand(Route = "create")]
                                         public sealed record CreateOrder
                                         {
                                             public string CustomerName { get; init; } = string.Empty;
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("CreateOrderDto", StringComparison.Ordinal))
            ?.GetText()
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("/// <summary>", dtoSource, StringComparison.Ordinal);
        Assert.Contains("Request DTO", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should include properties from source command.
    /// </summary>
    [Fact]
    public void GeneratedDtoIncludesPropertiesFromCommand()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand(Route = "create")]
                                         public sealed record CreateOrder
                                         {
                                             public string CustomerName { get; init; } = string.Empty;
                                             public decimal Amount { get; init; }
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("CreateOrderDto", StringComparison.Ordinal))
            ?.GetText()
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("CustomerName", dtoSource, StringComparison.Ordinal);
        Assert.Contains("Amount", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should use correct naming convention with Dto suffix.
    /// </summary>
    [Fact]
    public void GeneratedDtoUsesCorrectNamingConvention()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand(Route = "create")]
                                         public sealed record CreateOrder
                                         {
                                             public string CustomerName { get; init; } = string.Empty;
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("CreateOrderDto", StringComparison.Ordinal))
            ?.GetText()
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("public sealed record CreateOrderDto", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated files should have auto-generated header.
    /// </summary>
    [Fact]
    public void GeneratedFilesHaveAutoGeneratedHeader()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand(Route = "create")]
                                         public sealed record CreateOrder
                                         {
                                             public string CustomerName { get; init; } = string.Empty;
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        foreach (SyntaxTree tree in runResult.GeneratedTrees)
        {
            string generatedCode = tree.GetText().ToString();
            Assert.Contains("// <auto-generated", generatedCode, StringComparison.Ordinal);
        }
    }

    /// <summary>
    ///     Generated files should have GeneratedCodeAttribute.
    /// </summary>
    [Fact]
    public void GeneratedFilesHaveGeneratedCodeAttribute()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand(Route = "create")]
                                         public sealed record CreateOrder
                                         {
                                             public string CustomerName { get; init; } = string.Empty;
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        foreach (SyntaxTree tree in runResult.GeneratedTrees)
        {
            string generatedCode = tree.GetText().ToString();
            Assert.Contains("[global::System.CodeDom.Compiler.GeneratedCode(", generatedCode, StringComparison.Ordinal);
        }
    }

    /// <summary>
    ///     Generated mapper should have Map method with ArgumentNullException check.
    /// </summary>
    [Fact]
    public void GeneratedMapperHasNullCheckInMapMethod()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand(Route = "create")]
                                         public sealed record CreateOrder
                                         {
                                             public string CustomerName { get; init; } = string.Empty;
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        string? mapperSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("CreateOrderDtoMapper", StringComparison.Ordinal))
            ?.GetText()
            .ToString();
        Assert.NotNull(mapperSource);
        Assert.Contains("ArgumentNullException.ThrowIfNull(input);", mapperSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated mapper should implement IMapper from DTO to Command.
    /// </summary>
    [Fact]
    public void GeneratedMapperImplementsIMapperFromDtoToCommand()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand(Route = "create")]
                                         public sealed record CreateOrder
                                         {
                                             public string CustomerName { get; init; } = string.Empty;
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        string? mapperSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("CreateOrderDtoMapper", StringComparison.Ordinal))
            ?.GetText()
            .ToString();
        Assert.NotNull(mapperSource);
        Assert.Contains("IMapper<CreateOrderDto, CreateOrder>", mapperSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generator should produce no output when no commands are present.
    /// </summary>
    [Fact]
    public void GeneratorProducesNoOutputWhenNoCommands()
    {
        const string source = """
                              namespace TestApp
                              {
                                  public class RegularClass
                                  {
                                      public string Name { get; set; }
                                  }
                              }
                              """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, source);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Generator should produce output when command is decorated with attribute.
    /// </summary>
    [Fact]
    public void GeneratorProducesOutputWhenCommandHasAttribute()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand(Route = "create")]
                                         public sealed record CreateOrder
                                         {
                                             public string CustomerName { get; init; } = string.Empty;
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);

        // Should generate: DTO, Mapper, and Aggregate-level mapper registrations
        Assert.Equal(3, runResult.GeneratedTrees.Length);
    }

    /// <summary>
    ///     Mapper namespace should include Mappers suffix.
    /// </summary>
    [Fact]
    public void MapperNamespaceIncludesMappersSuffix()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand(Route = "create")]
                                         public sealed record CreateOrder
                                         {
                                             public string CustomerName { get; init; } = string.Empty;
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        string? mapperSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("CreateOrderDtoMapper", StringComparison.Ordinal))
            ?.GetText()
            .ToString();
        Assert.NotNull(mapperSource);
        Assert.Contains(
            "namespace TestApp.Server.Controllers.Aggregates.Mappers;",
            mapperSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Mapper should use correct constructor syntax for positional records.
    /// </summary>
    [Fact]
    public void MapperReturnsNewCommandInstance()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand(Route = "create")]
                                         public sealed record CreateOrder
                                         {
                                             public string CustomerName { get; init; } = string.Empty;
                                         }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);
        string? mapperSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("CreateOrderDtoMapper", StringComparison.Ordinal))
            ?.GetText()
            .ToString();
        Assert.NotNull(mapperSource);
        Assert.Contains("return new", mapperSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Multiple commands from same aggregate should be grouped in one registrations file.
    /// </summary>
    [Fact]
    public void MultipleCommandsFromSameAggregateGroupedInOneRegistrationsFile()
    {
        const string commandSource = """
                                     using Mississippi.Inlet.Generators.Abstractions;

                                     namespace TestApp.Domain.Aggregates.Order.Commands
                                     {
                                         [GenerateCommand(Route = "create")]
                                         public sealed record CreateOrder
                                         {
                                             public string CustomerName { get; init; } = string.Empty;
                                         }

                                         [GenerateCommand(Route = "update")]
                                         public sealed record UpdateOrder
                                         {
                                             public decimal NewAmount { get; init; }
                                         }

                                         [GenerateCommand(Route = "cancel")]
                                         public sealed record CancelOrder { }
                                     }
                                     """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, commandSource);

        // 3 commands → 3 DTOs + 3 Mappers + 1 AggregateMapperRegistrations = 7 files
        Assert.Equal(7, runResult.GeneratedTrees.Length);
        int registrationCount = runResult.GeneratedTrees.Count(t => t.FilePath.Contains(
            "AggregateMapperRegistrations",
            StringComparison.Ordinal));
        Assert.Equal(1, registrationCount);
    }
}