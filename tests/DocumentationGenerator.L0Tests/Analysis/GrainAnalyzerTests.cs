using System;
using System.IO;

using Allure.Xunit.Attributes;

using Mississippi.DocumentationGenerator.Analysis;


namespace Mississippi.DocumentationGenerator.L0Tests.Analysis;

/// <summary>
///     Unit tests for GrainAnalyzer.
/// </summary>
[AllureParentSuite("Documentation Generator")]
[AllureSuite("Analysis")]
[AllureSubSuite("Grain Analyzer")]
public sealed class GrainAnalyzerTests : IDisposable
{
    private readonly string tempDir;
    private readonly GrainAnalyzer analyzer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GrainAnalyzerTests" /> class.
    /// </summary>
    public GrainAnalyzerTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"GrainAnalyzerTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        analyzer = new GrainAnalyzer();
    }

    /// <summary>
    ///     Disposes the test resources.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    /// <summary>
    ///     Tests that AnalyzeFile detects grain implementing IGrainBase.
    /// </summary>
    [Fact(DisplayName = "AnalyzeFile detects grain implementing IGrainBase")]
    public void AnalyzeFileDetectsGrain()
    {
        // Arrange
        string content = """
            using Orleans.Runtime;

            namespace Test.Grains;

            public class TestGrain : ITestGrain, IGrainBase
            {
                public IGrainContext GrainContext { get; }
            }
            """;

        string filePath = Path.Combine(tempDir, "TestGrain.cs");
        File.WriteAllText(filePath, content);

        // Act
        GrainInfo? result = analyzer.AnalyzeFile(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestGrain", result.ClassName);
        Assert.Equal("Test.Grains", result.Namespace);
    }

    /// <summary>
    ///     Tests that AnalyzeFile detects StatelessWorker attribute.
    /// </summary>
    [Fact(DisplayName = "AnalyzeFile detects StatelessWorker attribute")]
    public void AnalyzeFileDetectsStatelessWorker()
    {
        // Arrange
        string content = """
            using Orleans.Concurrency;
            using Orleans.Runtime;

            namespace Test.Grains;

            [StatelessWorker]
            public class StatelessGrain : IStatelessGrain, IGrainBase
            {
                public IGrainContext GrainContext { get; }
            }
            """;

        string filePath = Path.Combine(tempDir, "StatelessGrain.cs");
        File.WriteAllText(filePath, content);

        // Act
        GrainInfo? result = analyzer.AnalyzeFile(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsStatelessWorker);
    }

    /// <summary>
    ///     Tests that AnalyzeFile returns null for non-grain files.
    /// </summary>
    [Fact(DisplayName = "AnalyzeFile returns null for non-grain files")]
    public void AnalyzeFileReturnsNullForNonGrain()
    {
        // Arrange
        string content = """
            namespace Test.Services;

            public class RegularService
            {
                public void DoWork() { }
            }
            """;

        string filePath = Path.Combine(tempDir, "RegularService.cs");
        File.WriteAllText(filePath, content);

        // Act
        GrainInfo? result = analyzer.AnalyzeFile(filePath);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     Tests that AnalyzeFile detects grain interface name.
    /// </summary>
    [Fact(DisplayName = "AnalyzeFile detects grain interface name")]
    public void AnalyzeFileDetectsInterfaceName()
    {
        // Arrange
        string content = """
            using Orleans.Runtime;

            namespace Test.Grains;

            public class MySpecialGrain : IMySpecialGrain, IGrainBase
            {
                public IGrainContext GrainContext { get; }
            }
            """;

        string filePath = Path.Combine(tempDir, "MySpecialGrain.cs");
        File.WriteAllText(filePath, content);

        // Act
        GrainInfo? result = analyzer.AnalyzeFile(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("IMySpecialGrain", result.InterfaceName);
    }

    /// <summary>
    ///     Tests that DisplayName includes namespace and class name.
    /// </summary>
    [Fact(DisplayName = "DisplayName includes namespace and class name")]
    public void DisplayNameIncludesNamespaceAndClassName()
    {
        // Arrange
        string content = """
            using Orleans.Runtime;

            namespace My.Custom.Namespace;

            public class CustomGrain : ICustomGrain, IGrainBase
            {
                public IGrainContext GrainContext { get; }
            }
            """;

        string filePath = Path.Combine(tempDir, "CustomGrain.cs");
        File.WriteAllText(filePath, content);

        // Act
        GrainInfo? result = analyzer.AnalyzeFile(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("My.Custom.Namespace.CustomGrain", result.DisplayName);
    }
}
