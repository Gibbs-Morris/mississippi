using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json.Serialization;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;


namespace Mississippi.Inlet.Gateway.Generators.L0Tests;

/// <summary>
///     Tests for <see cref="ProjectionEndpointsGenerator" />.
/// </summary>
public class ProjectionEndpointsGeneratorTests
{
    /// <summary>
    ///     Minimal attribute stubs needed for compilation without referencing the full SDK.
    /// </summary>
    private const string AttributeStubs = """
                                          namespace Mississippi.Inlet.Generators.Abstractions
                                          {
                                              using System;

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateProjectionEndpointsAttribute : Attribute { }

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateAuthorizationAttribute : Attribute
                                              {
                                                  public string? Policy { get; set; }
                                                  public string? Roles { get; set; }
                                                  public string? AuthenticationSchemes { get; set; }
                                              }

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class GenerateAllowAnonymousAttribute : Attribute
                                              {
                                              }
                                          }

                                          namespace Mississippi.Inlet.Abstractions
                                          {
                                              using System;

                                              [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                                              public sealed class ProjectionPathAttribute : Attribute
                                              {
                                                  public ProjectionPathAttribute(string path) => Path = path;
                                                  public string Path { get; }
                                              }
                                          }
                                          """;

    /// <summary>
    ///     Executes emitted dictionary mappers and verifies associations, empty results, and unchanged primitives.
    /// </summary>
    private static void AssertDictionaryMapperResults(
        Compilation compilation,
        bool preserveStringComparer
    )
    {
        using MemoryStream assemblyStream = new();
        Assert.True(compilation.Emit(assemblyStream, cancellationToken: TestContext.Current.CancellationToken).Success);
        assemblyStream.Position = 0;
        AssemblyLoadContext loadContext = new("dictionary-mapper-regression", true);
        try
        {
            Assembly assembly = loadContext.LoadFromStream(assemblyStream);
            Type[] types = assembly.GetTypes();
            object source = Activator.CreateInstance(types.Single(type => type.Name == "CatalogProjection"))!;
            Type mapperType = types.Single(type => type.Name == "CatalogProjectionMapper");
            ConstructorInfo constructor = mapperType
                .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single();
            object[] dependencies = constructor.GetParameters()
                .Select(parameter => Activator.CreateInstance(
                    types.Single(type =>
                        type.Name == (parameter.ParameterType.GenericTypeArguments[1].Name + "Mapper")),
                    true)!)
                .ToArray();
            object mapper = constructor.Invoke(dependencies);
            object mapped = mapperType.GetMethod("Map")!.Invoke(mapper, [source])!;
            object sourceEntries = source.GetType().GetProperty("Entries")!.GetValue(source)!;
            object mappedEntries = mapped.GetType().GetProperty("Entries")!.GetValue(mapped)!;
            Assert.Equal(2, mappedEntries.GetType().GetProperty("Count")!.GetValue(mappedEntries));
            object empty = mapped.GetType().GetProperty("Empty")!.GetValue(mapped)!;
            Assert.Equal(0, empty.GetType().GetProperty("Count")!.GetValue(empty));
            Assert.Equal(
                sourceEntries.GetType().GetGenericTypeDefinition(),
                mappedEntries.GetType().GetGenericTypeDefinition());
            Type keyType = mappedEntries.GetType().GenericTypeArguments[0];
            object firstKey;
            object secondKey;
            if (keyType == typeof(string))
            {
                firstKey = preserveStringComparer ? "FIRST" : "first";
                secondKey = preserveStringComparer ? "SECOND" : "second";
            }
            else if (keyType.IsEnum)
            {
                firstKey = Enum.ToObject(keyType, 2);
                secondKey = Enum.ToObject(keyType, 7);
            }
            else
            {
                firstKey = Activator.CreateInstance(keyType)!;
                keyType.GetProperty("Code")!.SetValue(firstKey, "first");
                secondKey = Activator.CreateInstance(keyType)!;
                keyType.GetProperty("Code")!.SetValue(secondKey, "second");
            }

            PropertyInfo indexer = mappedEntries.GetType().GetProperty("Item")!;
            object firstValue = indexer.GetValue(mappedEntries, [firstKey])!;
            object secondValue = indexer.GetValue(mappedEntries, [secondKey])!;
            if (firstValue.GetType().Name == "EntryDto")
            {
                Assert.Equal(42m, firstValue.GetType().GetProperty("Amount")!.GetValue(firstValue));
                Assert.Equal(99m, secondValue.GetType().GetProperty("Amount")!.GetValue(secondValue));
            }
            else
            {
                Assert.Equal(
                    firstValue.GetType().IsEnum ? 2 : 42,
                    Convert.ToInt32(firstValue, CultureInfo.InvariantCulture));
                Assert.Equal(
                    secondValue.GetType().IsEnum ? 7 : 99,
                    Convert.ToInt32(secondValue, CultureInfo.InvariantCulture));
            }

            Assert.Same(
                source.GetType().GetProperty("Values")!.GetValue(source),
                mapped.GetType().GetProperty("Values")!.GetValue(mapped));
            TestContext.Current.TestOutputHelper?.WriteLine("RUNTIME_DICTIONARY_ASSOCIATIONS=PASS");
        }
        finally
        {
            loadContext.Unload();
        }
    }

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
            MetadataReference.CreateFromFile(Path.Join(runtimeDirectory, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Join(runtimeDirectory, "System.Collections.dll")),
            MetadataReference.CreateFromFile(Path.Join(runtimeDirectory, "System.Collections.Immutable.dll")),
            MetadataReference.CreateFromFile(Path.Join(runtimeDirectory, "System.Linq.dll")),
            MetadataReference.CreateFromFile(typeof(JsonRequiredAttribute).Assembly.Location),
        ];

        // Add netstandard if available (for compatibility)
        string netstandardPath = Path.Join(runtimeDirectory, "netstandard.dll");
        if (File.Exists(netstandardPath))
        {
            references.Add(MetadataReference.CreateFromFile(netstandardPath));
        }

        CSharpCompilation compilation = CSharpCompilation.Create(
            "TestAssembly",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));

        // Run the generator
        ProjectionEndpointsGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        return (outputCompilation, diagnostics, driver.GetRunResult());
    }

    /// <summary>
    ///     Generated controller should call base constructor correctly.
    /// </summary>
    [Fact]
    public void GeneratedControllerCallsBaseConstructorCorrectly()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(controllerSource);
        Assert.Contains(": base(uxProjectionGrainFactory, mapper, logger)", controllerSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated controller should have constructor with correct dependencies.
    /// </summary>
    [Fact]
    public void GeneratedControllerHasCorrectConstructorDependencies()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(controllerSource);
        Assert.Contains(
            "IUxProjectionGrainFactory uxProjectionGrainFactory",
            controllerSource,
            StringComparison.Ordinal);
        Assert.Contains(
            "IMapper<AccountBalanceProjection, AccountBalanceDto> mapper",
            controllerSource,
            StringComparison.Ordinal);
        Assert.Contains("ILogger<AccountBalanceController> logger", controllerSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated controller should have correct route attribute.
    /// </summary>
    [Fact]
    public void GeneratedControllerHasCorrectRouteAttribute()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(controllerSource);
        Assert.Contains(
            "[Route(\"api/projections/account-balance/{entityId}\")]",
            controllerSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated controller should have correct route for projection path.
    /// </summary>
    [Fact]
    public void GeneratedControllerHasCorrectRouteForProjectionPath()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.TransactionHistory
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("transactions/history")]
                                            public sealed record TransactionHistoryProjection
                                            {
                                                public int Count { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(controllerSource);
        Assert.Contains(
            "[Route(\"api/projections/transactions/history/{entityId}\")]",
            controllerSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated controller should have XML documentation.
    /// </summary>
    [Fact]
    public void GeneratedControllerHasXmlDocumentation()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(controllerSource);
        Assert.Contains("/// <summary>", controllerSource, StringComparison.Ordinal);
        Assert.Contains("Controller for the AccountBalance projection.", controllerSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated controller should inherit from UxProjectionControllerBase.
    /// </summary>
    [Fact]
    public void GeneratedControllerInheritsFromUxProjectionControllerBase()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(controllerSource);
        Assert.Contains(
            "UxProjectionControllerBase<AccountBalanceProjection, AccountBalanceDto>",
            controllerSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated controller should be sealed partial class.
    /// </summary>
    [Fact]
    public void GeneratedControllerIsSealedPartialClass()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(controllerSource);
        Assert.Contains(
            "public sealed partial class AccountBalanceController",
            controllerSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Dictionary keys and values generate compilable DTOs, registrations, and executable mapping.
    /// </summary>
    /// <param name="dictionaryType">The declared dictionary type.</param>
    /// <param name="supplyGlobalUsing">Whether the fixture isolates discovery from generated imports.</param>
    [Theory]
    [InlineData("System.Collections.Generic.Dictionary<string, Entry>", true)]
    [InlineData("System.Collections.Immutable.ImmutableDictionary<string, Entry>", true)]
    [InlineData("System.Collections.Generic.IDictionary<string, Entry>", true)]
    [InlineData("System.Collections.Generic.IReadOnlyDictionary<string, Entry>", true)]
    [InlineData("System.Collections.Immutable.IImmutableDictionary<string, Entry>", true)]
    [InlineData("System.Collections.Generic.Dictionary<Key, Entry>", true)]
    [InlineData("System.Collections.Immutable.ImmutableDictionary<Key, Entry>", true)]
    [InlineData("System.Collections.Generic.Dictionary<Key, int>", true)]
    [InlineData("System.Collections.Generic.Dictionary<EntryStatus, EntryStatus>", true)]
    [InlineData("System.Collections.Immutable.ImmutableDictionary<EntryStatus, EntryStatus>", true)]
    [InlineData("System.Collections.Generic.Dictionary<string, int>", true)]
    [InlineData("System.Collections.Immutable.ImmutableDictionary<string, int>", true)]
    [InlineData("System.Collections.Generic.Dictionary<string, Entry>", false)]
    public void GeneratedDictionaryDtoAndMapperCompileWithCustomValues(
        string dictionaryType,
        bool supplyGlobalUsing
    )
    {
        ArgumentNullException.ThrowIfNull(dictionaryType);
        string typeArguments = dictionaryType.Substring(dictionaryType.IndexOf('<', StringComparison.Ordinal) + 1)
            .TrimEnd('>');
        string[] arguments = typeArguments.Split(',');
        string keyType = arguments[0].Trim();
        string valueType = arguments[1].Trim();
        string firstKey = keyType switch
        {
            "string" => "\"first\"",
            "Key" => "new Key { Code = \"first\" }",
            var _ => "EntryStatus.New",
        };
        string secondKey = keyType switch
        {
            "string" => "\"second\"",
            "Key" => "new Key { Code = \"second\" }",
            var _ => "EntryStatus.Complete",
        };
        string firstValue = valueType switch
        {
            "Entry" => "new Entry { Amount = 42m }",
            "int" => "42",
            var _ => "EntryStatus.New",
        };
        string secondValue = valueType switch
        {
            "Entry" => "new Entry { Amount = 99m }",
            "int" => "99",
            var _ => "EntryStatus.Complete",
        };
        bool immutable = dictionaryType.StartsWith("System.Collections.Immutable.", StringComparison.Ordinal);
        string comparer = keyType == "string" ? "System.StringComparer.OrdinalIgnoreCase" : string.Empty;
        string initializer = immutable
            ? $"System.Collections.Immutable.ImmutableDictionary.Create<{typeArguments}>({comparer}).Add({firstKey}, {firstValue}).Add({secondKey}, {secondValue})"
            : $"new System.Collections.Generic.Dictionary<{typeArguments}>({comparer}) {{ [{firstKey}] = {firstValue}, [{secondKey}] = {secondValue} }}";
        string emptyInitializer = immutable
            ? $"System.Collections.Immutable.ImmutableDictionary<{typeArguments}>.Empty"
            : $"new System.Collections.Generic.Dictionary<{typeArguments}>()";
        string source = $$"""
                          {{(supplyGlobalUsing ? "global using System.Collections.Generic;" : string.Empty)}}
                          using Mississippi.Inlet.Generators.Abstractions;
                          using Mississippi.Inlet.Abstractions;

                          namespace TestApp.Domain.Projections.Catalog;

                          public sealed record Entry { public decimal Amount { get; init; } }
                          public sealed record Key { public string Code { get; init; } = string.Empty; }
                          public enum EntryStatus { New = 2, Complete = 7 }

                          [GenerateProjectionEndpoints]
                          [ProjectionPath("catalog")]
                          public sealed record CatalogProjection
                          {
                              public {{dictionaryType}} Entries { get; init; } = {{initializer}};
                              public {{dictionaryType}} Empty { get; init; } = {{emptyInitializer}};
                              public System.Collections.Generic.Dictionary<string, int> Values { get; init; } =
                                  new(System.StringComparer.OrdinalIgnoreCase) { ["keep"] = 7 };
                          }
                          """;
        const string mappingContracts = """
                                        namespace Microsoft.Extensions.DependencyInjection
                                        {
                                            public interface IServiceCollection { }
                                        }
                                        namespace Mississippi.Common.Abstractions.Mapping
                                        {
                                            public interface IMapper<in TFrom, out TTo> { TTo Map(TFrom input); }
                                            public interface IEnumerableMapper<in TFrom, out TTo> :
                                                IMapper<System.Collections.Generic.IEnumerable<TFrom>, System.Collections.Generic.IEnumerable<TTo>> { }
                                        }
                                        namespace Mississippi.Common.Abstractions.Mapping
                                        {
                                            using Microsoft.Extensions.DependencyInjection;
                                            using Mississippi.Common.Abstractions.Mapping;
                                            public static class MappingRegistrations
                                            {
                                                public static IServiceCollection AddMapper<TFrom, TTo, TMapper>(this IServiceCollection services)
                                                    where TMapper : class, IMapper<TFrom, TTo> => services;
                                                public static IServiceCollection AddIEnumerableMapper(this IServiceCollection services) => services;
                                            }
                                        }
                                        """;
        (Compilation output, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult result) =
            RunGenerator(AttributeStubs, mappingContracts, source);
        Compilation input = output.RemoveSyntaxTrees(result.GeneratedTrees);
        Assert.Empty(
            input.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        TestContext.Current.TestOutputHelper?.WriteLine($"INPUT_ERRORS=0;FIXTURE_GLOBAL_USING={supplyGlobalUsing}");
        Assert.Empty(diagnostics);
        Assert.All(result.Results, generatorResult => Assert.Null(generatorResult.Exception));
        SyntaxTree[] dtoAndMapperTrees = result.GeneratedTrees.Where(tree =>
                !tree.FilePath.Contains("Controller", StringComparison.Ordinal))
            .ToArray();
        Assert.NotEmpty(dtoAndMapperTrees);
        foreach (SyntaxTree tree in dtoAndMapperTrees)
        {
            TestContext.Current.TestOutputHelper?.WriteLine(
                $"{tree.FilePath}\n{tree.GetText(TestContext.Current.CancellationToken)}");
        }

        Compilation dtoAndMapperCompilation = input.AddSyntaxTrees(dtoAndMapperTrees);
        Assert.Empty(
            dtoAndMapperCompilation.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        INamedTypeSymbol projection =
            input.GetTypeByMetadataName("TestApp.Domain.Projections.Catalog.CatalogProjection")!;
        INamedTypeSymbol entries =
            (INamedTypeSymbol)projection.GetMembers("Entries").OfType<IPropertySymbol>().Single().Type;
        foreach (ITypeSymbol type in entries.TypeArguments.Where(type => type.ContainingNamespace.ToDisplayString()
                         .StartsWith("TestApp.", StringComparison.Ordinal))
                     .Distinct<ITypeSymbol>(SymbolEqualityComparer.Default))
        {
            Assert.Single(
                result.GeneratedTrees,
                tree => tree.FilePath.EndsWith(type.Name + "Dto.g.cs", StringComparison.Ordinal));
        }

        bool preserveStringComparer = dictionaryType.Contains("Dictionary<string, Entry>", StringComparison.Ordinal) &&
                                      !dictionaryType.Contains("IDictionary", StringComparison.Ordinal) &&
                                      !dictionaryType.Contains("IReadOnlyDictionary", StringComparison.Ordinal) &&
                                      !dictionaryType.Contains("IImmutableDictionary", StringComparison.Ordinal);
        AssertDictionaryMapperResults(dtoAndMapperCompilation, preserveStringComparer);
    }

    /// <summary>
    ///     Generated projection array DTOs and their mappers compile using the declared mapping contracts.
    /// </summary>
    [Fact]
    public void GeneratedDtoAndMapperCompileForCustomAndEnumArrays()
    {
        const string mappingContracts = """
                                        using System.Collections.Generic;

                                        namespace Mississippi.Common.Abstractions.Mapping;

                                        public interface IMapper<in TFrom, out TTo>
                                        {
                                            TTo Map(TFrom input);
                                        }

                                        public interface IEnumerableMapper<in TFrom, out TTo>
                                            : IMapper<IEnumerable<TFrom>, IEnumerable<TTo>> { }
                                        """;
        const string source = """
                              using Mississippi.Inlet.Generators.Abstractions;
                              using Mississippi.Inlet.Abstractions;

                              namespace TestApp.Domain.Projections.Array;

                              public sealed record Entry
                              {
                                  public decimal Amount { get; init; }
                              }

                              public enum EntryStatus { New, Complete }

                              [GenerateProjectionEndpoints]
                              [ProjectionPath("array")]
                              public sealed record ArrayProjection
                              {
                                  public Entry[] Entries { get; init; } = [];
                                  public EntryStatus[] Statuses { get; init; } = [];
                                  public int[] Values { get; init; } = [];
                              }
                              """;
        (Compilation output, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult result) =
            RunGenerator(AttributeStubs, mappingContracts, source);
        Compilation input = output.RemoveSyntaxTrees(result.GeneratedTrees);
        Assert.Empty(
            input.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.Empty(diagnostics);
        Assert.All(result.Results, generatorResult => Assert.Null(generatorResult.Exception));
        SyntaxTree[] dtoAndMapperTrees = result.GeneratedTrees.Where(tree =>
                !tree.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !tree.FilePath.Contains("Registrations", StringComparison.Ordinal))
            .ToArray();
        Assert.NotEmpty(dtoAndMapperTrees);
        foreach (SyntaxTree tree in dtoAndMapperTrees)
        {
            TestContext.Current.TestOutputHelper?.WriteLine(
                $"{tree.FilePath}\n{tree.GetText(TestContext.Current.CancellationToken)}");
        }

        Compilation dtoAndMapperCompilation = input.AddSyntaxTrees(dtoAndMapperTrees);
        Assert.Empty(
            dtoAndMapperCompilation.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        string mapper = Assert.Single(
                dtoAndMapperTrees,
                tree => tree.FilePath.EndsWith("ArrayProjectionMapper.g.cs", StringComparison.Ordinal))
            .GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.Contains("IEnumerableMapper<Entry, EntryDto>", mapper, StringComparison.Ordinal);
        Assert.Contains("IEnumerableMapper<EntryStatus, EntryStatusDto>", mapper, StringComparison.Ordinal);
        Assert.Contains("EntriesMapper.Map(source.Entries).ToArray()", mapper, StringComparison.Ordinal);
        Assert.Contains("StatusesMapper.Map(source.Statuses).ToArray()", mapper, StringComparison.Ordinal);
        Assert.Contains("Values = source.Values", mapper, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Collections of custom types should generate nested DTOs and mappers with their properties.
    /// </summary>
    [Fact]
    public void GeneratedDtoHandlesCollectionOfCustomTypes()
    {
        const string projectionSource = """
                                        using System.Collections.Immutable;
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            public sealed record TransactionRecord
                                            {
                                                public decimal Amount { get; init; }
                                                public string Description { get; init; } = string.Empty;
                                            }

                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public ImmutableArray<TransactionRecord> Transactions { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        Assert.Empty(diagnostics);
        SyntaxTree nestedDtoTree = Assert.Single(
            runResult.GeneratedTrees,
            tree => tree.FilePath.EndsWith("TransactionRecordDto.g.cs", StringComparison.Ordinal));
        string nestedDtoSource = nestedDtoTree.GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains("public sealed record TransactionRecordDto", nestedDtoSource, StringComparison.Ordinal);
        Assert.Contains("public required decimal Amount { get; init; }", nestedDtoSource, StringComparison.Ordinal);
        Assert.Contains("public required string Description { get; init; }", nestedDtoSource, StringComparison.Ordinal);
        SyntaxTree nestedMapperTree = Assert.Single(
            runResult.GeneratedTrees,
            tree => tree.FilePath.EndsWith("TransactionRecordDtoMapper.g.cs", StringComparison.Ordinal));
        string nestedMapperSource = nestedMapperTree.GetText(TestContext.Current.CancellationToken).ToString();
        Assert.Contains(
            "IMapper<TransactionRecord, TransactionRecordDto>",
            nestedMapperSource,
            StringComparison.Ordinal);
        Assert.Contains("Amount = source.Amount", nestedMapperSource, StringComparison.Ordinal);
        Assert.Contains("Description = source.Description", nestedMapperSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should handle DateTimeOffset properties.
    /// </summary>
    [Fact]
    public void GeneratedDtoHandlesDateTimeOffsetProperties()
    {
        const string projectionSource = """
                                        using System;
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.Timestamps
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("timestamps")]
                                            public sealed record TimestampProjection
                                            {
                                                public DateTimeOffset CreatedAt { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("TimestampDto", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("CreatedAt", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should handle Guid properties.
    /// </summary>
    [Fact]
    public void GeneratedDtoHandlesGuidProperties()
    {
        const string projectionSource = """
                                        using System;
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.Identifiers
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("identifiers")]
                                            public sealed record IdentifierProjection
                                            {
                                                public Guid Id { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("IdentifierDto", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("Id", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should handle nullable properties.
    /// </summary>
    [Fact]
    public void GeneratedDtoHandlesNullableProperties()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal? OptionalBalance { get; init; }
                                                public string? OptionalName { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("AccountBalanceDto", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("OptionalBalance", dtoSource, StringComparison.Ordinal);
        Assert.Contains("OptionalName", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should have JsonRequired attributes.
    /// </summary>
    [Fact]
    public void GeneratedDtoHasJsonRequiredAttributes()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("AccountBalanceDto", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("[JsonRequired]", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should include properties from source projection.
    /// </summary>
    [Fact]
    public void GeneratedDtoIncludesPropertiesFromProjection()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                                public string AccountName { get; init; } = string.Empty;
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("AccountBalanceDto", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("Balance", dtoSource, StringComparison.Ordinal);
        Assert.Contains("AccountName", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should be public sealed record with braces syntax.
    /// </summary>
    [Fact]
    public void GeneratedDtoIsPublicSealedRecordWithBraces()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.MultiProperty
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("multi-property")]
                                            public sealed record MultiPropertyProjection
                                            {
                                                public decimal Balance { get; init; }
                                                public string Name { get; init; } = string.Empty;
                                                public int Count { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MultiPropertyDto", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("public sealed record MultiPropertyDto", dtoSource, StringComparison.Ordinal);
        Assert.Contains("Balance", dtoSource, StringComparison.Ordinal);
        Assert.Contains("Name", dtoSource, StringComparison.Ordinal);
        Assert.Contains("Count", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should use correct naming convention.
    /// </summary>
    [Fact]
    public void GeneratedDtoUsesCorrectNamingConvention()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("AccountBalanceDto", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("public sealed record AccountBalanceDto", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTO should use required modifier for properties.
    /// </summary>
    [Fact]
    public void GeneratedDtoUsesRequiredModifierForProperties()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("AccountBalanceDto", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("public required decimal Balance", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated files should have auto-generated header.
    /// </summary>
    [Fact]
    public void GeneratedFilesHaveAutoGeneratedHeader()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        foreach (SyntaxTree tree in runResult.GeneratedTrees)
        {
            string generatedCode = tree.GetText(TestContext.Current.CancellationToken).ToString();
            Assert.Contains("// <auto-generated", generatedCode, StringComparison.Ordinal);
        }
    }

    /// <summary>
    ///     Generated files should have correct namespace transformation.
    /// </summary>
    [Fact]
    public void GeneratedFilesHaveCorrectNamespace()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(controllerSource);

        // Output namespace is derived from the compilation target root namespace.
        Assert.Contains("namespace TestAssembly.Controllers.Projections;", controllerSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated files should have GeneratedCodeAttribute.
    /// </summary>
    [Fact]
    public void GeneratedFilesHaveGeneratedCodeAttribute()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        foreach (SyntaxTree tree in runResult.GeneratedTrees)
        {
            string generatedCode = tree.GetText(TestContext.Current.CancellationToken).ToString();
            Assert.Contains("[global::System.CodeDom.Compiler.GeneratedCode(", generatedCode, StringComparison.Ordinal);
        }
    }

    /// <summary>
    ///     Generated files should have nullable enabled.
    /// </summary>
    [Fact]
    public void GeneratedFilesHaveNullableEnabled()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        IEnumerable<string> generatedCodes = runResult.GeneratedTrees.Select(tree => tree.GetText().ToString());
        foreach (string generatedCode in generatedCodes)
        {
            Assert.Contains("#nullable enable", generatedCode, StringComparison.Ordinal);
        }
    }

    /// <summary>
    ///     Generated mapper should have Map method with ArgumentNullException check.
    /// </summary>
    [Fact]
    public void GeneratedMapperHasNullCheckInMapMethod()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? mapperSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("AccountBalanceProjectionMapper", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Registration", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(mapperSource);
        Assert.Contains("ArgumentNullException.ThrowIfNull(source);", mapperSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated mapper should implement IMapper interface.
    /// </summary>
    [Fact]
    public void GeneratedMapperImplementsIMapperInterface()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? mapperSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("AccountBalanceProjectionMapper", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Registration", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(mapperSource);
        Assert.Contains("IMapper<AccountBalanceProjection, AccountBalanceDto>", mapperSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated mapper should be internal sealed class.
    /// </summary>
    [Fact]
    public void GeneratedMapperIsInternalSealedClass()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? mapperSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("AccountBalanceProjectionMapper", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Registration", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(mapperSource);
        Assert.Contains("internal sealed class AccountBalanceProjectionMapper", mapperSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated mapper should map properties correctly.
    /// </summary>
    [Fact]
    public void GeneratedMapperMapsPropertiesCorrectly()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                                public string AccountName { get; init; } = string.Empty;
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? mapperSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("AccountBalanceProjectionMapper", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Registration", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(mapperSource);
        Assert.Contains("Balance = source.Balance", mapperSource, StringComparison.Ordinal);
        Assert.Contains("AccountName = source.AccountName", mapperSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated mapper registrations should have correct extension method.
    /// </summary>
    [Fact]
    public void GeneratedMapperRegistrationsHasCorrectExtensionMethod()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? registrationsSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Registration", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(registrationsSource);
        Assert.Contains("AddAccountBalanceProjectionMappers", registrationsSource, StringComparison.Ordinal);
        Assert.Contains("this IServiceCollection services", registrationsSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated mapper registrations should register mapper with AddMapper.
    /// </summary>
    [Fact]
    public void GeneratedMapperRegistrationsUsesAddMapper()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? registrationsSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Registration", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(registrationsSource);
        Assert.Contains(
            "AddMapper<AccountBalanceProjection, AccountBalanceDto, AccountBalanceProjectionMapper>",
            registrationsSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated projection analysis should warn when authentication schemes metadata contains empty entries.
    /// </summary>
    [Fact]
    public void GeneratedProjectionAnalysisWarnsForMalformedAuthenticationSchemesMetadata()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            [GenerateAuthorization(AuthenticationSchemes = "Bearer,,ApiKey")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        Assert.Contains(
            runResult.Diagnostics,
            diagnostic => (diagnostic.Id == "INLETAUTH001") && (diagnostic.Severity == DiagnosticSeverity.Warning));
    }

    /// <summary>
    ///     Generated projection controller should include both allow-anonymous and authorize metadata when both are
    ///     configured.
    /// </summary>
    [Fact]
    public void GeneratedProjectionControllerIncludesAllowAnonymousAndAuthorizeWhenBothConfigured()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            [GenerateAuthorization(Policy = "projection-read")]
                                            [GenerateAllowAnonymous]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(controllerSource);
        Assert.Contains("[AllowAnonymous]", controllerSource, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Policy = \"projection-read\")]", controllerSource, StringComparison.Ordinal);
        Assert.DoesNotContain(runResult.Diagnostics, diagnostic => diagnostic.Id == "INLETAUTH003");
    }

    /// <summary>
    ///     Generated projection controller should include allow-anonymous metadata when configured.
    /// </summary>
    [Fact]
    public void GeneratedProjectionControllerIncludesAllowAnonymousMetadataWhenConfigured()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            [GenerateAllowAnonymous]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(controllerSource);
        Assert.Contains("[AllowAnonymous]", controllerSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated projection controller should include authorize metadata when configured.
    /// </summary>
    [Fact]
    public void GeneratedProjectionControllerIncludesAuthorizeMetadataWhenConfigured()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            [GenerateAuthorization(Policy = "projection-read", Roles = "reader")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(controllerSource);
        Assert.Contains(
            "[Authorize(Policy = \"projection-read\", Roles = \"reader\")]",
            controllerSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated projection controller should remain auth-neutral by default.
    /// </summary>
    [Fact]
    public void GeneratedProjectionControllerRemainsAuthNeutralByDefault()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? controllerSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("Controller", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Mapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(controllerSource);
        Assert.DoesNotContain("[Authorize", controllerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("[AllowAnonymous]", controllerSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated DTOs should include enum DTOs and mappers for top-level enum properties.
    /// </summary>
    [Fact]
    public void GeneratedProjectionIncludesEnumDtoAndMapper()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.Sagas
                                        {
                                            public enum SagaPhase
                                            {
                                                NotStarted = 0,
                                                Running = 1,
                                            }

                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("saga-status")]
                                            public sealed record SagaStatusProjection
                                            {
                                                public SagaPhase Phase { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? enumDtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("SagaPhaseDto", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(enumDtoSource);
        Assert.Contains("public enum SagaPhaseDto", enumDtoSource, StringComparison.Ordinal);
        string? enumMapperSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("SagaPhaseDtoMapper", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(enumMapperSource);
        Assert.Contains("IMapper<SagaPhase, SagaPhaseDto>", enumMapperSource, StringComparison.Ordinal);
        string? registrationsSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("ProjectionMapperRegistrations", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(registrationsSource);
        Assert.Contains(
            "AddMapper<SagaPhase, SagaPhaseDto, SagaPhaseDtoMapper>();",
            registrationsSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated registrations should be internal static class.
    /// </summary>
    [Fact]
    public void GeneratedRegistrationsIsInternalStaticClass()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? registrationsSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Registration", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(registrationsSource);
        Assert.Contains(
            "internal static class AccountBalanceProjectionMapperRegistrations",
            registrationsSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generated registrations should return services for method chaining.
    /// </summary>
    [Fact]
    public void GeneratedRegistrationsReturnsServicesForChaining()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? registrationsSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("Registration", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(registrationsSource);
        Assert.Contains("return services;", registrationsSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generator should handle projection with no properties.
    /// </summary>
    [Fact]
    public void GeneratorHandlesProjectionWithNoProperties()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.Empty
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("empty")]
                                            public sealed record EmptyProjection;
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);

        // Should generate 4 files (DTO, Mapper, Registrations, Controller)
        Assert.Equal(4, runResult.GeneratedTrees.Length);
    }

    /// <summary>
    ///     Generator should ignore static properties.
    /// </summary>
    [Fact]
    public void GeneratorIgnoresStaticProperties()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.WithStatic
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("with-static")]
                                            public sealed record WithStaticProjection
                                            {
                                                public static string StaticValue { get; } = "static";
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? dtoSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("WithStaticDto", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(dtoSource);
        Assert.Contains("Balance", dtoSource, StringComparison.Ordinal);
        Assert.DoesNotContain("StaticValue", dtoSource, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Generator should produce no output when no projections are present.
    /// </summary>
    [Fact]
    public void GeneratorProducesNoOutputWhenNoProjections()
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
    ///     Generator should produce no output when projection has GenerateProjectionEndpoints but no ProjectionPath.
    /// </summary>
    [Fact]
    public void GeneratorProducesNoOutputWhenProjectionMissingProjectionPath()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        Assert.Empty(runResult.GeneratedTrees);
    }

    /// <summary>
    ///     Generator should produce output when projection has both required attributes.
    /// </summary>
    [Fact]
    public void GeneratorProducesOutputWhenProjectionHasBothAttributes()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);

        // Should generate: DTO, Mapper, MapperRegistrations, Controller
        Assert.Equal(4, runResult.GeneratedTrees.Length);
    }

    /// <summary>
    ///     Mapper namespace should include Mappers suffix.
    /// </summary>
    [Fact]
    public void MapperNamespaceIncludesMappersSuffix()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);
        string? mapperSource = runResult.GeneratedTrees.FirstOrDefault(t =>
                t.FilePath.Contains("AccountBalanceProjectionMapper", StringComparison.Ordinal) &&
                !t.FilePath.Contains("Registration", StringComparison.Ordinal))
            ?.GetText(TestContext.Current.CancellationToken)
            .ToString();
        Assert.NotNull(mapperSource);
        Assert.Contains(
            "namespace TestAssembly.Controllers.Projections.Mappers;",
            mapperSource,
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     Multiple projections should generate separate controllers.
    /// </summary>
    [Fact]
    public void MultipleProjectionsGenerateSeparateControllers()
    {
        const string projectionSource = """
                                        using Mississippi.Inlet.Generators.Abstractions;
                                        using Mississippi.Inlet.Abstractions;

                                        namespace TestApp.Domain.Projections.AccountBalance
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("account-balance")]
                                            public sealed record AccountBalanceProjection
                                            {
                                                public decimal Balance { get; init; }
                                            }
                                        }

                                        namespace TestApp.Domain.Projections.TransactionHistory
                                        {
                                            [GenerateProjectionEndpoints]
                                            [ProjectionPath("transaction-history")]
                                            public sealed record TransactionHistoryProjection
                                            {
                                                public int TransactionCount { get; init; }
                                            }
                                        }
                                        """;
        (Compilation _, ImmutableArray<Diagnostic> _, GeneratorDriverRunResult runResult) =
            RunGenerator(AttributeStubs, projectionSource);

        // Each projection generates 4 files (DTO, Mapper, Registrations, Controller)
        Assert.Equal(8, runResult.GeneratedTrees.Length);
        bool hasAccountBalanceController = runResult.GeneratedTrees.Any(t =>
            t.FilePath.Contains("AccountBalanceController", StringComparison.Ordinal));
        bool hasTransactionHistoryController = runResult.GeneratedTrees.Any(t =>
            t.FilePath.Contains("TransactionHistoryController", StringComparison.Ordinal));
        Assert.True(hasAccountBalanceController);
        Assert.True(hasTransactionHistoryController);
    }
}