using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Allure.Xunit.Attributes;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.DocumentationGenerator.Configuration;
using Mississippi.DocumentationGenerator.Infrastructure;
using Mississippi.DocumentationGenerator.Reports;


namespace Mississippi.DocumentationGenerator.L0Tests;

/// <summary>
///     Tests for <see cref="ProjectDependencyReport" />.
/// </summary>
[AllureParentSuite("Mississippi.DocumentationGenerator")]
[AllureSuite("Reports")]
[AllureSubSuite("ProjectDependencies")]
public sealed class ProjectDependencyReportTests : IDisposable
{
    private readonly string testDir;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ProjectDependencyReportTests" /> class.
    /// </summary>
    public ProjectDependencyReportTests()
    {
        testDir = Path.Combine(Path.GetTempPath(), $"docgen-deps-{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDir);
    }

    /// <summary>
    ///     Disposes test resources.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(testDir))
        {
            Directory.Delete(testDir, true);
        }
    }

    /// <summary>
    ///     Report should have correct name.
    /// </summary>
    [Fact]
    [AllureFeature("Report Metadata")]
    public void ReportHasCorrectName()
    {
        // Arrange
        ProjectDependencyReport report = CreateReport();

        // Assert
        report.Name.Should().Be("ProjectDependencies");
    }

    /// <summary>
    ///     ExecuteAsync should generate index and solution pages.
    /// </summary>
    [Fact]
    [AllureFeature("Report Execution")]
    public async Task ExecuteAsyncGeneratesExpectedFiles()
    {
        // Arrange
        string repoDir = CreateTestSolution();
        string outputDir = Path.Combine(testDir, "output");
        Directory.CreateDirectory(outputDir);

        ProjectDependencyReport report = CreateReport();
        DeterministicWriter writer = new();
        ReportContext context = new(
            repoDir,
            outputDir,
            new[] { Path.Combine(repoDir, "test.slnx") },
            writer
        );

        // Act
        await report.ExecuteAsync(context, CancellationToken.None);

        // Assert
        File.Exists(Path.Combine(outputDir, "dependencies", "index.mdx")).Should().BeTrue();
        File.Exists(Path.Combine(outputDir, "dependencies", "test-project-references.mdx")).Should().BeTrue();
    }

    /// <summary>
    ///     ExecuteAsync should produce deterministic output.
    /// </summary>
    [Fact]
    [AllureFeature("Determinism")]
    public async Task ExecuteAsyncProducesDeterministicOutput()
    {
        // Arrange
        string repoDir = CreateTestSolution();
        string output1 = Path.Combine(testDir, "output1");
        string output2 = Path.Combine(testDir, "output2");
        Directory.CreateDirectory(output1);
        Directory.CreateDirectory(output2);

        ProjectDependencyReport report = CreateReport();
        DeterministicWriter writer = new();
        ReportContext context1 = new(
            repoDir,
            output1,
            new[] { Path.Combine(repoDir, "test.slnx") },
            writer
        );
        ReportContext context2 = new(
            repoDir,
            output2,
            new[] { Path.Combine(repoDir, "test.slnx") },
            writer
        );

        // Act
        await report.ExecuteAsync(context1, CancellationToken.None);
        await report.ExecuteAsync(context2, CancellationToken.None);

        // Assert
        string[] files1 = Directory.GetFiles(output1, "*", SearchOption.AllDirectories);
        string[] files2 = Directory.GetFiles(output2, "*", SearchOption.AllDirectories);

        files1.Length.Should().Be(files2.Length);

        foreach (string file1 in files1)
        {
            string relativePath = Path.GetRelativePath(output1, file1);
            string file2 = Path.Combine(output2, relativePath);

            byte[] bytes1 = await File.ReadAllBytesAsync(file1);
            byte[] bytes2 = await File.ReadAllBytesAsync(file2);

            bytes1.Should().BeEquivalentTo(bytes2, $"File {relativePath} should be identical");
        }
    }

    /// <summary>
    ///     ExecuteAsync should detect project references.
    /// </summary>
    [Fact]
    [AllureFeature("Edge Detection")]
    public async Task ExecuteAsyncDetectsProjectReferences()
    {
        // Arrange
        string repoDir = CreateTestSolutionWithReferences();
        string outputDir = Path.Combine(testDir, "output");
        Directory.CreateDirectory(outputDir);

        ProjectDependencyReport report = CreateReport();
        DeterministicWriter writer = new();
        ReportContext context = new(
            repoDir,
            outputDir,
            new[] { Path.Combine(repoDir, "test.slnx") },
            writer
        );

        // Act
        await report.ExecuteAsync(context, CancellationToken.None);

        // Assert
        string content = await File.ReadAllTextAsync(
            Path.Combine(outputDir, "dependencies", "test-project-references.mdx")
        );
        content.Should().Contain("ProjectA");
        content.Should().Contain("ProjectB");
        content.Should().Contain("-->"); // Mermaid edge syntax
    }

    private string CreateTestSolution()
    {
        string repoDir = Path.Combine(testDir, "repo");
        Directory.CreateDirectory(repoDir);

        // Create solution file
        File.WriteAllText(
            Path.Combine(repoDir, "test.slnx"),
            """
            <Solution>
                <Project Path="src\ProjectA\ProjectA.csproj" />
            </Solution>
            """
        );

        // Create project directory and file
        string projectDir = Path.Combine(repoDir, "src", "ProjectA");
        Directory.CreateDirectory(projectDir);
        File.WriteAllText(
            Path.Combine(projectDir, "ProjectA.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                </PropertyGroup>
            </Project>
            """
        );

        return repoDir;
    }

    private string CreateTestSolutionWithReferences()
    {
        string repoDir = Path.Combine(testDir, "repo-refs");
        Directory.CreateDirectory(repoDir);

        // Create solution file
        File.WriteAllText(
            Path.Combine(repoDir, "test.slnx"),
            """
            <Solution>
                <Project Path="src\ProjectA\ProjectA.csproj" />
                <Project Path="src\ProjectB\ProjectB.csproj" />
            </Solution>
            """
        );

        // Create ProjectA
        string projectADir = Path.Combine(repoDir, "src", "ProjectA");
        Directory.CreateDirectory(projectADir);
        File.WriteAllText(
            Path.Combine(projectADir, "ProjectA.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                </PropertyGroup>
            </Project>
            """
        );

        // Create ProjectB with reference to ProjectA
        string projectBDir = Path.Combine(repoDir, "src", "ProjectB");
        Directory.CreateDirectory(projectBDir);
        File.WriteAllText(
            Path.Combine(projectBDir, "ProjectB.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                </PropertyGroup>
                <ItemGroup>
                    <ProjectReference Include="..\ProjectA\ProjectA.csproj" />
                </ItemGroup>
            </Project>
            """
        );

        return repoDir;
    }

    private static ProjectDependencyReport CreateReport()
    {
        return new ProjectDependencyReport(
            Options.Create(new ProjectDependencyOptions()),
            NullLogger<ProjectDependencyReport>.Instance
        );
    }
}
