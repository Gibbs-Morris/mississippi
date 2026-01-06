using System;
using System.Collections.Generic;
using System.IO;

using Allure.Xunit.Attributes;

using Mississippi.DocumentationGenerator.Analysis;


namespace Mississippi.DocumentationGenerator.L0Tests.Analysis;

/// <summary>
///     Unit tests for ProjectAnalyzer.
/// </summary>
[AllureParentSuite("Documentation Generator")]
[AllureSuite("Analysis")]
[AllureSubSuite("Project Analyzer")]
public sealed class ProjectAnalyzerTests : IDisposable
{
    private readonly string tempDir;
    private readonly ProjectAnalyzer analyzer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectAnalyzerTests" /> class.
    /// </summary>
    public ProjectAnalyzerTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"ProjectAnalyzerTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        analyzer = new ProjectAnalyzer();
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
    ///     Tests that ParseSolution extracts project paths from slnx file.
    /// </summary>
    [Fact(DisplayName = "ParseSolution extracts project paths from slnx file")]
    public void ParseSolutionExtractsProjectPaths()
    {
        // Arrange
        string solutionContent = """
            <Solution>
              <Folder Name="/Projects/">
                <Project Path="src\Project1\Project1.csproj" />
                <Project Path="src\Project2\Project2.csproj" />
              </Folder>
            </Solution>
            """;

        string solutionPath = Path.Combine(tempDir, "test.slnx");
        File.WriteAllText(solutionPath, solutionContent);

        string project1Dir = Path.Combine(tempDir, "src", "Project1");
        string project2Dir = Path.Combine(tempDir, "src", "Project2");
        Directory.CreateDirectory(project1Dir);
        Directory.CreateDirectory(project2Dir);
        File.WriteAllText(Path.Combine(project1Dir, "Project1.csproj"), "<Project></Project>");
        File.WriteAllText(Path.Combine(project2Dir, "Project2.csproj"), "<Project></Project>");

        // Act
        IReadOnlyList<string> result = analyzer.ParseSolution(solutionPath);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.EndsWith("Project1.csproj", StringComparison.Ordinal));
        Assert.Contains(result, p => p.EndsWith("Project2.csproj", StringComparison.Ordinal));
    }

    /// <summary>
    ///     Tests that AnalyzeProject extracts project information.
    /// </summary>
    [Fact(DisplayName = "AnalyzeProject extracts project information")]
    public void AnalyzeProjectExtractsInformation()
    {
        // Arrange
        string projectContent = """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\Dependency\Dependency.csproj" />
                <PackageReference Include="TestPackage" />
              </ItemGroup>
            </Project>
            """;

        string projectDir = Path.Combine(tempDir, "src", "TestProject");
        Directory.CreateDirectory(projectDir);
        string projectPath = Path.Combine(projectDir, "TestProject.csproj");
        File.WriteAllText(projectPath, projectContent);

        // Act
        ProjectInfo result = analyzer.AnalyzeProject(projectPath);

        // Assert
        Assert.Equal("TestProject", result.Name);
        Assert.True(result.IsConsoleApp);
        Assert.False(result.IsTestProject);
        Assert.Single(result.ProjectReferences);
        Assert.Contains("Dependency", result.ProjectReferences);
        Assert.Single(result.PackageReferences);
        Assert.Contains("TestPackage", result.PackageReferences);
    }

    /// <summary>
    ///     Tests that AnalyzeProject identifies test projects.
    /// </summary>
    [Fact(DisplayName = "AnalyzeProject identifies test projects")]
    public void AnalyzeProjectIdentifiesTestProjects()
    {
        // Arrange
        string projectContent = "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>";

        string projectDir = Path.Combine(tempDir, "tests", "MyProject.L0Tests");
        Directory.CreateDirectory(projectDir);
        string projectPath = Path.Combine(projectDir, "MyProject.L0Tests.csproj");
        File.WriteAllText(projectPath, projectContent);

        // Act
        ProjectInfo result = analyzer.AnalyzeProject(projectPath);

        // Assert
        Assert.True(result.IsTestProject);
    }

    /// <summary>
    ///     Tests that ParseSolution returns sorted results.
    /// </summary>
    [Fact(DisplayName = "ParseSolution returns sorted results")]
    public void ParseSolutionReturnsSortedResults()
    {
        // Arrange
        string solutionContent = """
            <Solution>
              <Project Path="src\Zebra\Zebra.csproj" />
              <Project Path="src\Alpha\Alpha.csproj" />
              <Project Path="src\Beta\Beta.csproj" />
            </Solution>
            """;

        string solutionPath = Path.Combine(tempDir, "test.slnx");
        File.WriteAllText(solutionPath, solutionContent);

        string[] projects = { "Alpha", "Beta", "Zebra" };
        foreach (string name in projects)
        {
            string dir = Path.Combine(tempDir, "src", name);
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, $"{name}.csproj"), "<Project></Project>");
        }

        // Act
        IReadOnlyList<string> result = analyzer.ParseSolution(solutionPath);

        // Assert - should be alphabetically sorted
        Assert.Equal(3, result.Count);
        Assert.True(result[0].Contains("Alpha", StringComparison.Ordinal));
        Assert.True(result[1].Contains("Beta", StringComparison.Ordinal));
        Assert.True(result[2].Contains("Zebra", StringComparison.Ordinal));
    }
}
