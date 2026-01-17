using System;
using System.IO;

using Allure.Xunit.Attributes;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Mississippi.DocumentationGenerator.Configuration;
using Mississippi.DocumentationGenerator.Discovery;


namespace Mississippi.DocumentationGenerator.L0Tests;

/// <summary>
///     Tests for <see cref="PathDiscovery" />.
/// </summary>
[AllureParentSuite("Mississippi.DocumentationGenerator")]
[AllureSuite("Discovery")]
[AllureSubSuite("PathDiscovery")]
public sealed class PathDiscoveryTests : IDisposable
{
    private readonly string testDir;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PathDiscoveryTests" /> class.
    /// </summary>
    public PathDiscoveryTests()
    {
        testDir = Path.Combine(Path.GetTempPath(), $"docgen-discovery-{Guid.NewGuid():N}");
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
    ///     GetRepoRoot should throw when configured path is invalid.
    /// </summary>
    [Fact]
    [AllureFeature("Repository Discovery")]
    public void GetRepoRootThrowsWhenConfiguredPathInvalid()
    {
        // Arrange
        DocGenOptions options = new() { RepoRoot = Path.Combine(testDir, "nonexistent") };
        PathDiscovery discovery = CreateDiscovery(options);

        // Act & Assert
        Action act = () => discovery.GetRepoRoot();
        act.Should().Throw<DocGenException>()
            .WithMessage("*invalid*");
    }

    /// <summary>
    ///     GetRepoRoot should accept configured path when valid.
    /// </summary>
    [Fact]
    [AllureFeature("Repository Discovery")]
    public void GetRepoRootAcceptsValidConfiguredPath()
    {
        // Arrange - create fake repo structure
        string repoDir = Path.Combine(testDir, "repo");
        Directory.CreateDirectory(repoDir);
        File.WriteAllText(Path.Combine(repoDir, "mississippi.slnx"), "<Solution/>");
        File.WriteAllText(Path.Combine(repoDir, "samples.slnx"), "<Solution/>");

        DocGenOptions options = new() { RepoRoot = repoDir };
        PathDiscovery discovery = CreateDiscovery(options);

        // Act
        string result = discovery.GetRepoRoot();

        // Assert
        result.Should().Be(repoDir);
    }

    /// <summary>
    ///     GetDocusaurusRoot should throw when configured path is invalid.
    /// </summary>
    [Fact]
    [AllureFeature("Docusaurus Discovery")]
    public void GetDocusaurusRootThrowsWhenConfiguredPathInvalid()
    {
        // Arrange
        string repoDir = CreateValidRepoStructure();
        DocGenOptions options = new()
        {
            RepoRoot = repoDir,
            DocusaurusRoot = Path.Combine(testDir, "invalid-docusaurus")
        };
        PathDiscovery discovery = CreateDiscovery(options);

        // Act & Assert
        Action act = () => discovery.GetDocusaurusRoot();
        act.Should().Throw<DocGenException>()
            .WithMessage("*invalid*");
    }

    /// <summary>
    ///     GetDocusaurusRoot should find default location.
    /// </summary>
    [Fact]
    [AllureFeature("Docusaurus Discovery")]
    public void GetDocusaurusRootFindsDefaultLocation()
    {
        // Arrange
        string repoDir = CreateValidRepoStructure();
        string docusaurusDir = Path.Combine(repoDir, "docs", "Docusaurus");
        Directory.CreateDirectory(docusaurusDir);
        File.WriteAllText(Path.Combine(docusaurusDir, "docusaurus.config.ts"), "export default {};");
        File.WriteAllText(Path.Combine(docusaurusDir, "package.json"), "{}");

        DocGenOptions options = new() { RepoRoot = repoDir };
        PathDiscovery discovery = CreateDiscovery(options);

        // Act
        string result = discovery.GetDocusaurusRoot();

        // Assert
        result.Should().Be(docusaurusDir);
    }

    /// <summary>
    ///     GetDocusaurusRoot should scan for non-default location.
    /// </summary>
    [Fact]
    [AllureFeature("Docusaurus Discovery")]
    public void GetDocusaurusRootScansForNonDefaultLocation()
    {
        // Arrange
        string repoDir = CreateValidRepoStructure();
        string altDocusaurusDir = Path.Combine(repoDir, "docs", "website");
        Directory.CreateDirectory(altDocusaurusDir);
        File.WriteAllText(Path.Combine(altDocusaurusDir, "docusaurus.config.js"), "module.exports = {};");
        File.WriteAllText(Path.Combine(altDocusaurusDir, "package.json"), "{}");

        DocGenOptions options = new() { RepoRoot = repoDir };
        PathDiscovery discovery = CreateDiscovery(options);

        // Act
        string result = discovery.GetDocusaurusRoot();

        // Assert
        result.Should().Be(altDocusaurusDir);
    }

    /// <summary>
    ///     GetDocsContentRoot should return docs subdirectory.
    /// </summary>
    [Fact]
    [AllureFeature("Docs Content Discovery")]
    public void GetDocsContentRootReturnsDocsSubdirectory()
    {
        // Arrange
        string repoDir = CreateValidRepoStructure();
        string docusaurusDir = Path.Combine(repoDir, "docs", "Docusaurus");
        string docsContentDir = Path.Combine(docusaurusDir, "docs");
        Directory.CreateDirectory(docusaurusDir);
        Directory.CreateDirectory(docsContentDir);
        File.WriteAllText(Path.Combine(docusaurusDir, "docusaurus.config.ts"), "export default {};");
        File.WriteAllText(Path.Combine(docusaurusDir, "package.json"), "{}");

        DocGenOptions options = new() { RepoRoot = repoDir };
        PathDiscovery discovery = CreateDiscovery(options);

        // Act
        string result = discovery.GetDocsContentRoot();

        // Assert
        result.Should().Be(docsContentDir);
    }

    /// <summary>
    ///     GetOutputDir should use configured path if provided.
    /// </summary>
    [Fact]
    [AllureFeature("Output Directory")]
    public void GetOutputDirUsesConfiguredPath()
    {
        // Arrange
        string outputDir = Path.Combine(testDir, "custom-output");
        DocGenOptions options = new() { OutputDir = outputDir };
        PathDiscovery discovery = CreateDiscovery(options);

        // Act
        string result = discovery.GetOutputDir();

        // Assert
        result.Should().Be(outputDir);
    }

    private string CreateValidRepoStructure()
    {
        string repoDir = Path.Combine(testDir, "repo");
        Directory.CreateDirectory(repoDir);
        File.WriteAllText(Path.Combine(repoDir, "mississippi.slnx"), "<Solution/>");
        File.WriteAllText(Path.Combine(repoDir, "samples.slnx"), "<Solution/>");
        return repoDir;
    }

    private static PathDiscovery CreateDiscovery(
        DocGenOptions options
    )
    {
        return new PathDiscovery(
            Options.Create(options),
            NullLogger<PathDiscovery>.Instance
        );
    }
}
