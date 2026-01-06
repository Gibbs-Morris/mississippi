using System;
using System.IO;

using Allure.Xunit.Attributes;

using Mississippi.DocumentationGenerator.Discovery;


namespace Mississippi.DocumentationGenerator.L0Tests.Discovery;

/// <summary>
///     Unit tests for RepositoryDiscovery.
/// </summary>
[AllureParentSuite("Documentation Generator")]
[AllureSuite("Discovery")]
[AllureSubSuite("Repository Discovery")]
public sealed class RepositoryDiscoveryTests : IDisposable
{
    private readonly string tempDir;
    private readonly RepositoryDiscovery discovery;

    /// <summary>
    ///     Initializes a new instance of the <see cref="RepositoryDiscoveryTests" /> class.
    /// </summary>
    public RepositoryDiscoveryTests()
    {
        tempDir = Path.Combine(Path.GetTempPath(), $"RepositoryDiscoveryTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        discovery = new RepositoryDiscovery();
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
    ///     Tests that DiscoverRepoRoot finds repo when solution files exist.
    /// </summary>
    [Fact(DisplayName = "DiscoverRepoRoot finds repository when solution files exist")]
    public void DiscoverRepoRootFindsSolutionFiles()
    {
        // Arrange
        string repoDir = Path.Combine(tempDir, "repo");
        Directory.CreateDirectory(repoDir);
        File.WriteAllText(Path.Combine(repoDir, "mississippi.slnx"), "<Solution></Solution>");
        File.WriteAllText(Path.Combine(repoDir, "samples.slnx"), "<Solution></Solution>");

        string subDir = Path.Combine(repoDir, "src", "project");
        Directory.CreateDirectory(subDir);

        // Act
        string result = discovery.DiscoverRepoRoot(subDir);

        // Assert
        Assert.Equal(repoDir, result);
    }

    /// <summary>
    ///     Tests that DiscoverRepoRoot throws when no solution files found.
    /// </summary>
    [Fact(DisplayName = "DiscoverRepoRoot throws when no solution files found")]
    public void DiscoverRepoRootThrowsWhenNoSolutionFiles()
    {
        // Arrange
        string emptyDir = Path.Combine(tempDir, "empty");
        Directory.CreateDirectory(emptyDir);

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            discovery.DiscoverRepoRoot(emptyDir));

        Assert.Contains("mississippi.slnx", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Tests that DiscoverDocusaurusRoot finds valid Docusaurus directory.
    /// </summary>
    [Fact(DisplayName = "DiscoverDocusaurusRoot finds valid Docusaurus directory")]
    public void DiscoverDocusaurusRootFindsValidDirectory()
    {
        // Arrange
        string repoDir = Path.Combine(tempDir, "repo");
        string docusaurusDir = Path.Combine(repoDir, "docs", "Docusaurus");
        Directory.CreateDirectory(docusaurusDir);
        File.WriteAllText(Path.Combine(docusaurusDir, "docusaurus.config.ts"), "export default {};");
        File.WriteAllText(Path.Combine(docusaurusDir, "package.json"), "{}");

        // Act
        string result = discovery.DiscoverDocusaurusRoot(repoDir);

        // Assert
        Assert.Equal(docusaurusDir, result);
    }

    /// <summary>
    ///     Tests that DiscoverDocusaurusRoot throws when no valid directory found.
    /// </summary>
    [Fact(DisplayName = "DiscoverDocusaurusRoot throws when no valid directory found")]
    public void DiscoverDocusaurusRootThrowsWhenNotFound()
    {
        // Arrange
        string repoDir = Path.Combine(tempDir, "repo");
        Directory.CreateDirectory(repoDir);

        // Act & Assert
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            discovery.DiscoverDocusaurusRoot(repoDir));

        Assert.Contains("Docusaurus", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Tests that DiscoverDocusaurusDocsRoot finds docs directory.
    /// </summary>
    [Fact(DisplayName = "DiscoverDocusaurusDocsRoot finds docs directory")]
    public void DiscoverDocusaurusDocsRootFindsDocsDirectory()
    {
        // Arrange
        string docusaurusDir = Path.Combine(tempDir, "Docusaurus");
        string docsDir = Path.Combine(docusaurusDir, "docs");
        Directory.CreateDirectory(docsDir);

        // Act
        string result = discovery.DiscoverDocusaurusDocsRoot(docusaurusDir);

        // Assert
        Assert.Equal(docsDir, result);
    }

    /// <summary>
    ///     Tests that GetDefaultSolutionFiles returns correct paths.
    /// </summary>
    [Fact(DisplayName = "GetDefaultSolutionFiles returns correct paths")]
    public void GetDefaultSolutionFilesReturnsCorrectPaths()
    {
        // Arrange
        string repoDir = "/test/repo";

        // Act
        string[] result = RepositoryDiscovery.GetDefaultSolutionFiles(repoDir);

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Contains(result, p => p.EndsWith("mississippi.slnx", StringComparison.Ordinal));
        Assert.Contains(result, p => p.EndsWith("samples.slnx", StringComparison.Ordinal));
    }
}
