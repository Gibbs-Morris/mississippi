using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;

using Mississippi.Inlet.Client.Generators;


namespace MississippiTests.Inlet.Client.Generators.L0Tests;

/// <summary>
///     Verifies that projection enum DTOs retain the declared integer type and values.
/// </summary>
public sealed class ProjectionEnumUnderlyingTypeTests
{
    private const string FrameworkStubs = """
                                          namespace Mississippi.Inlet.Generators.Abstractions
                                          {
                                              public sealed class GenerateProjectionEndpointsAttribute : System.Attribute { }
                                          }
                                          namespace Mississippi.Inlet.Abstractions
                                          {
                                              public sealed class ProjectionPathAttribute(string path) : System.Attribute { }
                                          }
                                          """;

    private static readonly string[] FrameworkAssemblyNames =
    {
        "System.Runtime.dll", "System.Collections.dll", "System.Collections.Immutable.dll", "netstandard.dll",
    };

    /// <summary>
    ///     Valid enums and their generated client DTOs compile without losing integer width or signedness.
    /// </summary>
    /// <param name="underlyingType">The source enum's integer type, or empty for default int.</param>
    /// <param name="members">Valid source declarations, including boundary values.</param>
    /// <param name="isCollection">Whether the projection exposes enum array elements.</param>
    [Theory]
    [InlineData("", "Minimum = int.MinValue, Ordinary = 17, Maximum = int.MaxValue", false)]
    [InlineData("int", "Minimum = int.MinValue, Ordinary = 17, Maximum = int.MaxValue", false)]
    [InlineData("sbyte", "Minimum = sbyte.MinValue, Ordinary = 17, Maximum = sbyte.MaxValue", false)]
    [InlineData("byte", "Minimum = byte.MinValue, Ordinary = 17, Maximum = byte.MaxValue", false)]
    [InlineData("short", "Minimum = short.MinValue, Ordinary = 17, Maximum = short.MaxValue", false)]
    [InlineData("ushort", "Minimum = ushort.MinValue, Ordinary = 17, Maximum = ushort.MaxValue", false)]
    [InlineData(
        "long",
        "Minimum = long.MinValue, BelowInt = -2147483649L, Ordinary = 17L, AboveInt = 2147483648L, Maximum = long.MaxValue",
        false)]
    [InlineData(
        "ulong",
        "Minimum = 0UL, Ordinary = 17UL, AboveInt = 2147483648UL, AboveLong = 9223372036854775808UL, Maximum = ulong.MaxValue",
        false)]
    [InlineData("uint", "Minimum = 0U, Ordinary = 17U, AboveInt = 2147483648U, Maximum = uint.MaxValue", false)]
    [InlineData("long", "Minimum = long.MinValue, Ordinary = 17L, Maximum = long.MaxValue", true)]
    [InlineData("ulong", "Minimum = 0UL, Ordinary = 17UL, Maximum = ulong.MaxValue", true)]
    [InlineData("uint", "Minimum = 0U, Ordinary = 17U, Maximum = uint.MaxValue", true)]
    public void GeneratedEnumPreservesUnderlyingTypeAndConstants(
        string underlyingType,
        string members,
        bool isCollection
    )
    {
        ArgumentNullException.ThrowIfNull(underlyingType);
        ArgumentNullException.ThrowIfNull(members);
        string baseClause = underlyingType.Length == 0 ? string.Empty : " : " + underlyingType;
        string property = isCollection
            ? "public Status[] Values { get; init; } = [];"
            : "public Status Value { get; init; }";
        string source = $$"""
                          using Mississippi.Inlet.Generators.Abstractions;
                          using Mississippi.Inlet.Abstractions;

                          namespace TestApp.Domain.Projections.Sample;

                          public enum Status{{baseClause}} { {{members}} }

                          [GenerateProjectionEndpoints]
                          [ProjectionPath("sample")]
                          public sealed record SampleProjection
                          {
                              {{property}}
                          }
                          """;
        MetadataReference[] references = FrameworkAssemblyNames
            .Select(name => MetadataReference.CreateFromFile(
                Path.Join(Path.GetDirectoryName(typeof(object).Assembly.Location)!, name)))
            .Append(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .ToArray();
        CSharpCompilation input = CSharpCompilation.Create(
            "TestApp.Client",
            [
                CSharpSyntaxTree.ParseText(FrameworkStubs, cancellationToken: TestContext.Current.CancellationToken),
                CSharpSyntaxTree.ParseText(source, cancellationToken: TestContext.Current.CancellationToken),
            ],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(
                NullableContextOptions.Enable));
        Assert.Empty(
            input.GetDiagnostics(TestContext.Current.CancellationToken)
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ProjectionClientDtoGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(
            input,
            out Compilation output,
            out ImmutableArray<Diagnostic> diagnostics,
            TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        Assert.Empty(diagnostics);
        Assert.All(result.Results, generatorResult => Assert.Null(generatorResult.Exception));
        SyntaxTree enumTree = Assert.Single(
            result.GeneratedTrees,
            tree => tree.FilePath.EndsWith("StatusDto.g.cs", StringComparison.Ordinal));
        TestContext.Current.TestOutputHelper?.WriteLine(
            enumTree.GetText(TestContext.Current.CancellationToken).ToString());
        Diagnostic[] errors = output.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        TestContext.Current.TestOutputHelper?.WriteLine(
            string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
        Assert.Empty(errors);
        INamedTypeSymbol sourceEnum = Assert.IsType<INamedTypeSymbol>(
            input.GetTypeByMetadataName("TestApp.Domain.Projections.Sample.Status"),
            false);
        EnumDeclarationSyntax declaration = Assert.Single(
            enumTree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<EnumDeclarationSyntax>());
        INamedTypeSymbol dtoEnum = Assert.IsType<INamedTypeSymbol>(
            output.GetSemanticModel(enumTree).GetDeclaredSymbol(declaration, TestContext.Current.CancellationToken),
            false);
        Assert.Equal(sourceEnum.EnumUnderlyingType!.SpecialType, dtoEnum.EnumUnderlyingType!.SpecialType);
        IFieldSymbol[] sourceMembers = sourceEnum.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(member => member.HasConstantValue)
            .ToArray();
        IFieldSymbol[] dtoMembers = dtoEnum.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(member => member.HasConstantValue)
            .ToArray();
        Assert.Equal(sourceMembers.Length, dtoMembers.Length);
        foreach (IFieldSymbol member in sourceMembers)
        {
            IFieldSymbol dtoMember = Assert.Single(dtoMembers, candidate => candidate.Name == member.Name);
            Assert.Equal(member.ConstantValue, dtoMember.ConstantValue);
        }

        using MemoryStream stream = new();
        EmitResult emit = output.Emit(stream, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
    }
}