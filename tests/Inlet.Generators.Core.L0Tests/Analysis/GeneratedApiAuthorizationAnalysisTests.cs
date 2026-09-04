using System;
using System.Globalization;
using System.IO;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Mississippi.Inlet.Generators.Core.Analysis;


namespace Mississippi.Inlet.Generators.Core.L0Tests.Analysis;

/// <summary>
///     Tests for generated API authorization metadata analysis.
/// </summary>
public sealed class GeneratedApiAuthorizationAnalysisTests
{
    /// <summary>
    ///     Analyze should trim list entries, omit empty entries, and diagnose the malformed roles list.
    /// </summary>
    [Fact]
    public void AnalyzeNormalizesListsAndDiagnosesEmptyRoleEntries()
    {
        const string source = """
                              using System;

                              [AttributeUsage(AttributeTargets.Class)]
                              public sealed class GenerateAuthorizationAttribute : Attribute
                              {
                                  public string Roles { get; set; } = string.Empty;
                                  public string AuthenticationSchemes { get; set; } = string.Empty;
                              }

                              [GenerateAuthorization(Roles = " reader , , writer ", AuthenticationSchemes = " Bearer , ApiKey ")]
                              public sealed class Endpoint { }
                              """;
        string? runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location);
        Assert.NotNull(runtimeDirectory);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "AuthorizationTests",
            [CSharpSyntaxTree.ParseText(source)],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Runtime.dll")),
            ],
            new(OutputKind.DynamicallyLinkedLibrary));
        Assert.Empty(compilation.GetDiagnostics());
        INamedTypeSymbol? typeSymbol = compilation.GetTypeByMetadataName("Endpoint");
        INamedTypeSymbol? authorizationAttribute = compilation.GetTypeByMetadataName("GenerateAuthorizationAttribute");
        Assert.NotNull(typeSymbol);
        Assert.NotNull(authorizationAttribute);
        GeneratedApiAuthorizationModel result = GeneratedApiAuthorizationAnalysis.Analyze(
            typeSymbol,
            authorizationAttribute,
            null,
            false);
        Assert.Equal("reader,writer", result.Roles);
        Assert.Equal("Bearer,ApiKey", result.AuthenticationSchemes);
        Diagnostic diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(GeneratedApiAuthorizationAnalysis.MalformedListMetadata, diagnostic.Descriptor);
        Assert.Contains("Roles", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }
}