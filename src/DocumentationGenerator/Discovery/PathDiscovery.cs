using System;
using System.IO;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Mississippi.DocumentationGenerator.Configuration;


namespace Mississippi.DocumentationGenerator.Discovery;

/// <summary>
///     Handles autodiscovery of repository root, Docusaurus root, and docs content paths.
/// </summary>
public sealed class PathDiscovery
{
    private ILogger<PathDiscovery> Logger { get; }

    private DocGenOptions Options { get; }

    private string? cachedRepoRoot;

    private string? cachedDocusaurusRoot;

    private string? cachedDocsContentRoot;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PathDiscovery" /> class.
    /// </summary>
    /// <param name="options">Configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public PathDiscovery(
        IOptions<DocGenOptions> options,
        ILogger<PathDiscovery> logger
    )
    {
        Options = options.Value;
        Logger = logger;
    }

    /// <summary>
    ///     Discovers the repository root path.
    /// </summary>
    /// <returns>The repository root path.</returns>
    /// <exception cref="DocGenException">Thrown when repository root cannot be found.</exception>
    public string GetRepoRoot()
    {
        if (cachedRepoRoot != null)
        {
            return cachedRepoRoot;
        }

        if (!string.IsNullOrEmpty(Options.RepoRoot))
        {
            string configuredPath = Path.GetFullPath(Options.RepoRoot);
            if (ValidateRepoRoot(configuredPath))
            {
                cachedRepoRoot = configuredPath;
                Logger.LogInformation("Using configured repository root: {RepoRoot}", cachedRepoRoot);
                return cachedRepoRoot;
            }

            throw new DocGenException(
                $"Configured repository root '{configuredPath}' is invalid. " +
                "Must contain both mississippi.slnx and samples.slnx."
            );
        }

        // Autodiscovery: walk up from current directory
        string currentDir = Environment.CurrentDirectory;
        Logger.LogDebug("Starting repository autodiscovery from: {CurrentDir}", currentDir);

        while (!string.IsNullOrEmpty(currentDir))
        {
            if (ValidateRepoRoot(currentDir))
            {
                cachedRepoRoot = currentDir;
                Logger.LogInformation("Discovered repository root: {RepoRoot}", cachedRepoRoot);
                return cachedRepoRoot;
            }

            // Check for .git directory as fallback
            string gitDir = Path.Combine(currentDir, ".git");
            if (Directory.Exists(gitDir) && ValidateRepoRoot(currentDir))
            {
                cachedRepoRoot = currentDir;
                Logger.LogInformation("Discovered repository root via .git: {RepoRoot}", cachedRepoRoot);
                return cachedRepoRoot;
            }

            string? parent = Directory.GetParent(currentDir)?.FullName;
            if (parent == currentDir)
            {
                break;
            }

            currentDir = parent ?? string.Empty;
        }

        throw new DocGenException(
            "Failed to discover repository root. " +
            "Ensure you run from within the Mississippi repository, or specify --repoRoot explicitly. " +
            "Repository root must contain both mississippi.slnx and samples.slnx."
        );
    }

    /// <summary>
    ///     Discovers the Docusaurus root path.
    /// </summary>
    /// <returns>The Docusaurus root path.</returns>
    /// <exception cref="DocGenException">Thrown when Docusaurus root cannot be found.</exception>
    public string GetDocusaurusRoot()
    {
        if (cachedDocusaurusRoot != null)
        {
            return cachedDocusaurusRoot;
        }

        if (!string.IsNullOrEmpty(Options.DocusaurusRoot))
        {
            string configuredPath = Path.GetFullPath(Options.DocusaurusRoot);
            if (ValidateDocusaurusRoot(configuredPath))
            {
                cachedDocusaurusRoot = configuredPath;
                Logger.LogInformation("Using configured Docusaurus root: {DocusaurusRoot}", cachedDocusaurusRoot);
                return cachedDocusaurusRoot;
            }

            throw new DocGenException(
                $"Configured Docusaurus root '{configuredPath}' is invalid. " +
                "Must contain docusaurus.config.* and package.json."
            );
        }

        // Try default location: {RepoRoot}/docs/Docusaurus
        string repoRoot = GetRepoRoot();
        string defaultPath = Path.Combine(repoRoot, "docs", "Docusaurus");

        if (ValidateDocusaurusRoot(defaultPath))
        {
            cachedDocusaurusRoot = defaultPath;
            Logger.LogInformation("Found Docusaurus at default location: {DocusaurusRoot}", cachedDocusaurusRoot);
            return cachedDocusaurusRoot;
        }

        // Fallback: scan docs/ for docusaurus.config.*
        string docsDir = Path.Combine(repoRoot, "docs");
        if (Directory.Exists(docsDir))
        {
            foreach (string subDir in Directory.GetDirectories(docsDir, "*", SearchOption.AllDirectories))
            {
                if (ValidateDocusaurusRoot(subDir))
                {
                    cachedDocusaurusRoot = subDir;
                    Logger.LogInformation("Found Docusaurus via scan: {DocusaurusRoot}", cachedDocusaurusRoot);
                    return cachedDocusaurusRoot;
                }
            }
        }

        throw new DocGenException(
            "Failed to discover Docusaurus root. " +
            "Expected at docs/Docusaurus with docusaurus.config.* and package.json. " +
            "Use --docusaurusRoot to specify explicitly."
        );
    }

    /// <summary>
    ///     Gets the Docusaurus docs content root.
    /// </summary>
    /// <returns>The docs content root path.</returns>
    /// <exception cref="DocGenException">Thrown when docs content root cannot be determined.</exception>
    public string GetDocsContentRoot()
    {
        if (cachedDocsContentRoot != null)
        {
            return cachedDocsContentRoot;
        }

        string docusaurusRoot = GetDocusaurusRoot();
        string docsPath = Path.Combine(docusaurusRoot, "docs");

        if (Directory.Exists(docsPath))
        {
            cachedDocsContentRoot = docsPath;
            Logger.LogInformation("Found docs content root: {DocsContentRoot}", cachedDocsContentRoot);
            return cachedDocsContentRoot;
        }

        throw new DocGenException(
            $"Docs content directory not found at '{docsPath}'. " +
            "Expected {DocusaurusRoot}/docs to exist."
        );
    }

    /// <summary>
    ///     Gets the output directory for generated docs.
    /// </summary>
    /// <returns>The output directory path.</returns>
    public string GetOutputDir()
    {
        if (!string.IsNullOrEmpty(Options.OutputDir))
        {
            return Path.GetFullPath(Options.OutputDir);
        }

        return Path.Combine(GetDocsContentRoot(), "generated");
    }

    private static bool ValidateRepoRoot(
        string path
    )
    {
        if (!Directory.Exists(path))
        {
            return false;
        }

        return File.Exists(Path.Combine(path, "mississippi.slnx"))
               && File.Exists(Path.Combine(path, "samples.slnx"));
    }

    private static bool ValidateDocusaurusRoot(
        string path
    )
    {
        if (!Directory.Exists(path))
        {
            return false;
        }

        // Check for docusaurus.config.* (ts, js, mjs)
        bool hasConfig = File.Exists(Path.Combine(path, "docusaurus.config.ts"))
                         || File.Exists(Path.Combine(path, "docusaurus.config.js"))
                         || File.Exists(Path.Combine(path, "docusaurus.config.mjs"));

        bool hasPackageJson = File.Exists(Path.Combine(path, "package.json"));

        return hasConfig && hasPackageJson;
    }
}
