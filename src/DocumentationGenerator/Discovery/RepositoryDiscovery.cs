using System;
using System.IO;


namespace Mississippi.DocumentationGenerator.Discovery;

/// <summary>
///     Provides methods for discovering repository structure and paths.
/// </summary>
public sealed class RepositoryDiscovery
{
    private const string MississippiSolutionFile = "mississippi.slnx";
    private const string SamplesSolutionFile = "samples.slnx";
    private const string GitDirectory = ".git";
    private const string DocusaurusDefaultPath = "docs/Docusaurus";
    private const string DocusaurusConfigPattern = "docusaurus.config.*";
    private const string PackageJsonFile = "package.json";
    private const string DocsFolderName = "docs";

    /// <summary>
    ///     Discovers the repository root by walking upward from the start path.
    /// </summary>
    /// <param name="startPath">The path to start searching from.</param>
    /// <returns>The repository root path.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the repository root cannot be found.</exception>
    public string DiscoverRepoRoot(
        string? startPath = null
    )
    {
        string currentPath = Path.GetFullPath(startPath ?? Directory.GetCurrentDirectory());

        while (!string.IsNullOrEmpty(currentPath))
        {
            // Check if both solution files exist
            string mississippiPath = Path.Combine(currentPath, MississippiSolutionFile);
            string samplesPath = Path.Combine(currentPath, SamplesSolutionFile);

            if (File.Exists(mississippiPath) && File.Exists(samplesPath))
            {
                return currentPath;
            }

            // Check for .git directory and then look for solution files
            string gitPath = Path.Combine(currentPath, GitDirectory);
            if (Directory.Exists(gitPath))
            {
                // Found .git, now verify solution files exist
                if (File.Exists(mississippiPath) && File.Exists(samplesPath))
                {
                    return currentPath;
                }

                // Found .git but no solution files - continue searching upward
            }

            string? parentPath = Path.GetDirectoryName(currentPath);
            if (string.IsNullOrEmpty(parentPath) || parentPath == currentPath)
            {
                break;
            }

            currentPath = parentPath;
        }

        throw new InvalidOperationException(
            $"Unable to locate repository root from '{startPath ?? Directory.GetCurrentDirectory()}'. " +
            $"Expected to find both '{MississippiSolutionFile}' and '{SamplesSolutionFile}' or a .git directory.");
    }

    /// <summary>
    ///     Discovers the Docusaurus root directory.
    /// </summary>
    /// <param name="repoRoot">The repository root path.</param>
    /// <returns>The Docusaurus root path.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the Docusaurus root cannot be found.</exception>
    public string DiscoverDocusaurusRoot(
        string repoRoot
    )
    {
        ArgumentNullException.ThrowIfNull(repoRoot);

        // First, check the expected default path: docs/Docusaurus
        string defaultPath = Path.Combine(repoRoot, DocusaurusDefaultPath);
        if (IsValidDocusaurusRoot(defaultPath))
        {
            return defaultPath;
        }

        // Scan under docs/ for docusaurus.config.*
        string docsPath = Path.Combine(repoRoot, DocsFolderName);
        if (Directory.Exists(docsPath))
        {
            foreach (string directory in Directory.GetDirectories(docsPath, "*", SearchOption.AllDirectories))
            {
                if (IsValidDocusaurusRoot(directory))
                {
                    return directory;
                }
            }
        }

        throw new InvalidOperationException(
            $"Unable to locate Docusaurus root. Expected 'docs/Docusaurus' with '{DocusaurusConfigPattern}' and '{PackageJsonFile}', " +
            $"or a subdirectory under 'docs/' containing these files. " +
            $"Searched from repository root: '{repoRoot}'.");
    }

    /// <summary>
    ///     Discovers the Docusaurus docs content root directory.
    /// </summary>
    /// <param name="docusaurusRoot">The Docusaurus root path.</param>
    /// <returns>The Docusaurus docs content path.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the docs content root cannot be found.</exception>
    public string DiscoverDocusaurusDocsRoot(
        string docusaurusRoot
    )
    {
        ArgumentNullException.ThrowIfNull(docusaurusRoot);

        // Check for {DocusaurusRoot}/docs
        string docsPath = Path.Combine(docusaurusRoot, DocsFolderName);
        if (Directory.Exists(docsPath))
        {
            return docsPath;
        }

        throw new InvalidOperationException(
            $"Unable to locate Docusaurus docs content directory. " +
            $"Expected '{DocsFolderName}' directory under Docusaurus root: '{docusaurusRoot}'.");
    }

    /// <summary>
    ///     Gets the default solution files for the repository.
    /// </summary>
    /// <param name="repoRoot">The repository root path.</param>
    /// <returns>A list of default solution file paths.</returns>
    public static string[] GetDefaultSolutionFiles(
        string repoRoot
    )
    {
        ArgumentNullException.ThrowIfNull(repoRoot);

        return new[]
        {
            Path.Combine(repoRoot, MississippiSolutionFile),
            Path.Combine(repoRoot, SamplesSolutionFile),
        };
    }

    private static bool IsValidDocusaurusRoot(
        string path
    )
    {
        if (!Directory.Exists(path))
        {
            return false;
        }

        // Check for docusaurus.config.* (supports .js, .ts, .mjs, etc.)
        string[] configFiles = Directory.GetFiles(path, DocusaurusConfigPattern);
        if (configFiles.Length == 0)
        {
            return false;
        }

        // Check for package.json
        string packageJsonPath = Path.Combine(path, PackageJsonFile);
        return File.Exists(packageJsonPath);
    }
}
